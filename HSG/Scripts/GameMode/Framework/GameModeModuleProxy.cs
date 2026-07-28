using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Virial.DI;
using Virial.Game;

namespace hvtXsvc.GameMode.Framework;

/// <summary>
/// 运行时代理 — 通过 System.Reflection.Emit 动态生成实现 IGameModeModule 的类型，
/// 绕过 IModuleContainer.AddModule 为 internal 无法在编译期实现的限制。
/// </summary>
internal static class GameModeModuleProxy
{
    private static ModuleBuilder? _moduleBuilder;
    private static int _typeCounter;
    // 缓存已生成的代理类型
    private static readonly Dictionary<Type, Type> _proxyTypeCache = new();

    /// <summary>
    /// 创建 IGameModeModule 代理实例，将接口调用委托给目标对象
    /// </summary>
    internal static IGameModeModule Create(object target)
    {
        var proxyType = GetOrCreateProxyType(target.GetType());
        var proxy = Activator.CreateInstance(proxyType)!;
        // 设置 _target 字段
        proxyType.GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(proxy, target);
        return (IGameModeModule)proxy;
    }

    /// <summary>
    /// 将模块工厂注册到 DIManager.allContainers
    /// </summary>
    internal static void RegisterModuleType(Type moduleType, Func<object> factory)
    {
        var instanceProp = typeof(DIManager).GetProperty("Instance",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var instance = instanceProp?.GetValue(null);
        if (instance == null) return;

        var containersField = typeof(DIManager).GetField("allContainers",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var containers = containersField?.GetValue(instance) as IDictionary;
        if (containers == null) return;

        containers[moduleType] = new Func<object>(() =>
        {
            var raw = factory();
            return Create(raw);
        });
    }

    // ===== TypeBuilder 动态类型生成 =====

    private static Type GetOrCreateProxyType(Type targetType)
    {
        if (_proxyTypeCache.TryGetValue(targetType, out var cachedType))
            return cachedType;

        // 获取或创建 ModuleBuilder
        if (_moduleBuilder == null)
        {
            var assemblyName = new AssemblyName("HSG_GameModeProxy_Dynamic");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = assemblyBuilder.DefineDynamicModule("ProxyModule");
        }

        var typeName = $"Proxy_{targetType.Name}_{Interlocked.Increment(ref _typeCounter)}";
        var typeBuilder = _moduleBuilder.DefineType(typeName,
            TypeAttributes.Public | TypeAttributes.Class);

        // 实现接口
        typeBuilder.AddInterfaceImplementation(typeof(IGameModeModule));

        // 添加 _target 字段
        var targetField = typeBuilder.DefineField("_target", typeof(object),
            FieldAttributes.Private);

        // 获取 IModuleContainer.AddModule 的 MethodInfo（internal 方法）
        var addModuleMethod = typeof(IModuleContainer).GetMethod("AddModule",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        // 获取 IModuleContainer.GetModule<T> 的 MethodInfo
        var getModuleMethod = typeof(IModuleContainer).GetMethod("GetModule",
            BindingFlags.Instance | BindingFlags.Public);

        // 实现 AddModule — 空操作
        if (addModuleMethod != null)
            EmitEmptyMethod(typeBuilder, addModuleMethod);

        // 实现 GetModule<T> — 返回 null
        if (getModuleMethod != null)
            EmitNullReturnMethod(typeBuilder, getModuleMethod);

        // 遍历 IGameModeModule 的成员，对没有默认实现的成员进行代理
        var handledMethods = new HashSet<string> { "AddModule", "GetModule" };
        foreach (var method in typeof(IGameModeModule).GetMethods(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (handledMethods.Contains(method.Name)) continue;
            handledMethods.Add(method.Name);

            // 省略带默认实现的方法（如 ShowMap => true）
            // 仅处理抽象方法（无默认实现）— 但 DIM 运行时也可能需要显式实现
            EmitDelegateMethod(typeBuilder, method, targetField, targetType);
        }

        foreach (var prop in typeof(IGameModeModule).GetProperties(
            BindingFlags.Instance | BindingFlags.Public))
        {
            var getter = prop.GetGetMethod();
            if (getter != null && !handledMethods.Contains(getter.Name))
            {
                handledMethods.Add(getter.Name);
                EmitDelegateMethod(typeBuilder, getter, targetField, targetType);
            }
        }

        var createdType = typeBuilder.CreateType()!;
        _proxyTypeCache[targetType] = createdType;
        return createdType;
    }

    /// <summary>生成空方法体（仅返回）</summary>
    private static void EmitEmptyMethod(TypeBuilder typeBuilder, MethodInfo interfaceMethod)
    {
        var methodBuilder = typeBuilder.DefineMethod(
            interfaceMethod.Name,
            MethodAttributes.Private | MethodAttributes.Virtual |
            MethodAttributes.Final | MethodAttributes.HideBySig,
            interfaceMethod.ReturnType,
            interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray());

        var il = methodBuilder.GetILGenerator();
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);
    }

    /// <summary>生成返回 null 的方法</summary>
    private static void EmitNullReturnMethod(TypeBuilder typeBuilder, MethodInfo interfaceMethod)
    {
        var methodBuilder = typeBuilder.DefineMethod(
            interfaceMethod.Name,
            MethodAttributes.Private | MethodAttributes.Virtual |
            MethodAttributes.Final | MethodAttributes.HideBySig,
            interfaceMethod.ReturnType,
            interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray());

        var il = methodBuilder.GetILGenerator();
        // GetModule<T> 是泛型方法，需要特殊处理
        if (interfaceMethod.IsGenericMethodDefinition)
        {
            var genericParams = interfaceMethod.GetGenericArguments();
            var gParams = methodBuilder.DefineGenericParameters(
                genericParams.Select(g => g.Name).ToArray());
            // 设置泛型约束 where T : class, IModule
            foreach (var gp in gParams)
            {
                gp.SetBaseTypeConstraint(typeof(object));
                gp.SetInterfaceConstraints(typeof(IModule));
            }
        }

        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Ret);

        typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);
    }

    /// <summary>生成目标委托方法：从 _target 字段调用对应成员</summary>
    private static void EmitDelegateMethod(TypeBuilder typeBuilder, MethodInfo interfaceMethod,
        FieldInfo targetField, Type targetType)
    {
        var paramTypes = interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray();
        var methodBuilder = typeBuilder.DefineMethod(
            interfaceMethod.Name,
            MethodAttributes.Private | MethodAttributes.Virtual |
            MethodAttributes.Final | MethodAttributes.HideBySig,
            interfaceMethod.ReturnType,
            paramTypes);

        // 在目标类型上查找对应的方法或属性 getter
        var targetMember = FindTargetMember(interfaceMethod, targetType);
        if (targetMember == null)
        {
            // 目标没有该成员，返回默认值
            var il = methodBuilder.GetILGenerator();
            if (interfaceMethod.ReturnType != typeof(void))
            {
                if (interfaceMethod.ReturnType.IsValueType)
                {
                    var local = il.DeclareLocal(interfaceMethod.ReturnType);
                    il.Emit(OpCodes.Ldloca_S, local);
                    il.Emit(OpCodes.Initobj, interfaceMethod.ReturnType);
                    il.Emit(OpCodes.Ldloc, local);
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }
            }
            il.Emit(OpCodes.Ret);
        }
        else
        {
            var il = methodBuilder.GetILGenerator();
            // 加载 _target 字段
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, targetField);
            // 转换类型
            il.Emit(OpCodes.Castclass, targetType);
            // 调用目标方法
            il.Emit(OpCodes.Callvirt, targetMember);
            il.Emit(OpCodes.Ret);
        }

        typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);
    }

    /// <summary>在目标类型上查找与接口方法对应的成员</summary>
    private static MethodInfo? FindTargetMember(MethodInfo interfaceMethod, Type targetType)
    {
        var name = interfaceMethod.Name;
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // 先尝试同名方法
        var targetMethod = targetType.GetMethod(name, flags, null,
            interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray(), null);
        if (targetMethod != null && targetMethod.ReturnType == interfaceMethod.ReturnType)
            return targetMethod;

        // 如果是 get_ 开头，尝试属性
        if (name.StartsWith("get_"))
        {
            var propName = name[4..];
            var prop = targetType.GetProperty(propName, flags);
            if (prop != null)
            {
                var getter = prop.GetGetMethod(true);
                if (getter != null && getter.ReturnType == interfaceMethod.ReturnType)
                    return getter;
            }
        }

        return null;
    }
}
