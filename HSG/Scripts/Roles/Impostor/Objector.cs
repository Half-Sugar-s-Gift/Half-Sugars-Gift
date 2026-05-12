using System;
using System.Collections.Generic;
using System.Reflection;
using hvtXsvc.Core;
using Nebula.Modules;
using Nebula.Utilities;
using UnityEngine;
using Virial;
using Virial.Assignable;
using Virial.Configuration;
using Virial.Events.Game;
using Virial.Events.Game.Meeting;
using Virial.Events.Player;
using Virial.Game;
using Color = UnityEngine.Color;
using GamePlayer = Virial.Game.Player;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using TMPro;

namespace NebulaN.Scripts.Roles.Impostor;

public class Objector : DefinedRoleTemplate, HasCitation, DefinedRole,
    RuntimeAssignableGenerator<RuntimeRole>, IAssignableDocument
{

    static private IntegerConfiguration objectionUses = NebulaAPI.Configurations.Configuration(
        "options.role.objector.uses", (1, 15), 3, null, null);

    private Objector() : base("objector", new Virial.Color(Palette.ImpostorRed.r, Palette.ImpostorRed.g, Palette.ImpostorRed.b), RoleCategory.ImpostorRole,
        NebulaTeams.ImpostorTeam, new Virial.Configuration.IConfiguration[] { objectionUses })
    { }

    Citation? HasCitation.Citation => Citations.hvtXsvc_hsg;
    bool IAssignableDocument.HasTips => true;
    bool IAssignableDocument.HasAbility => true;

    IEnumerable<AssignableDocumentImage> IAssignableDocument.GetDocumentImages()
    {
        yield return new AssignableDocumentImage(
            NebulaAPI.AddonAsset.GetResource("ObjectorObjection.png")?.AsImage(115f),
            "role.objector.doc.objection");
        yield return new AssignableDocumentImage(
            NebulaAPI.AddonAsset.GetResource("ObjectorCancel.png")?.AsImage(115f),
            "role.objector.doc.cancel");
    }

    IEnumerable<AssignableDocumentReplacement> IAssignableDocument.GetDocumentReplacements()
    {
        yield return new AssignableDocumentReplacement("%USES%", ((int)objectionUses).ToString());
    }

    public static readonly Objector MyRole = new Objector();
    public RuntimeRole CreateInstance(GamePlayer player, int[] arguments) => new Instance(player);

    public class Instance : RuntimeAssignableTemplate, RuntimeRole, RuntimeAssignable, IGameOperator
    {
        void IGameOperator.OnReleased() { }
        IEnumerable<IPlayerAbility> RuntimeAssignable.MyAbilities => Array.Empty<IPlayerAbility>();
        public DefinedRole Role => MyRole;
        public Instance(GamePlayer player) : base(player) { }

        private int usesLeft;
        private bool isObjecting;
        private bool objectionUsedThisMeeting;

        private GameObject? binder;
        private SpriteRenderer? objectRenderer;
        private SpriteRenderer? cancelRenderer;
        private TextMeshPro? usesText;

        private static Virial.Media.Image? objImage = NebulaAPI.AddonAsset.GetResource("ObjectorObjection.png")?.AsImage(100f);
        private static Virial.Media.Image? cancelImage = NebulaAPI.AddonAsset.GetResource("ObjectorCancel.png")?.AsImage(100f);

        private static Sprite? GetSpriteFromImage(Virial.Media.Image? image)
        {
            if (image == null) return null;
            var method = typeof(Virial.Media.Image).GetMethod("GetSprite", BindingFlags.Instance | BindingFlags.NonPublic);
            return method?.Invoke(image, null) as Sprite;
        }

        void RuntimeAssignable.OnActivated()
        {
            if (!AmOwner) return;
            usesLeft = objectionUses;
        }

        [Local]
        private void OnMeetingStart(MeetingStartEvent ev)
        {
            if (!AmOwner || MyPlayer.IsDead || usesLeft <= 0) return;
            if (objImage == null || cancelImage == null) return;

            isObjecting = false;
            objectionUsedThisMeeting = false;
            binder = UnityHelper.CreateObject("ObjectorButtons",
                MeetingHud.Instance.SkipVoteButton.transform.parent,
                MeetingHud.Instance.SkipVoteButton.transform.localPosition);

            objectRenderer = UnityHelper.CreateObject<SpriteRenderer>(
                "ObjectorObject", binder.transform, new Vector3(2.1f, 0.15f, -0.5f));
            objectRenderer.sprite = GetSpriteFromImage(objImage);
            objectRenderer.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

            var objPassive = objectRenderer.gameObject.SetUpButton(true, new SpriteRenderer[0], null, null);
            objPassive.OnMouseOver.AddListener(() => objectRenderer.color = Color.green);
            objPassive.OnMouseOut.AddListener(() => objectRenderer.color = Color.white);
            objPassive.OnClick.AddListener(() =>
            {
                if (!isObjecting && usesLeft > 0)
                {
                    isObjecting = true;
                    UpdateUI();
                }
            });
            objectRenderer.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.6f);

            cancelRenderer = UnityHelper.CreateObject<SpriteRenderer>(
                "ObjectorCancel", binder.transform, new Vector3(2.76f, 0.15f, -0.5f));
            cancelRenderer.sprite = GetSpriteFromImage(cancelImage);
            cancelRenderer.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

            var cancelPassive = cancelRenderer.gameObject.SetUpButton(true, new SpriteRenderer[0], null, null);
            cancelPassive.OnMouseOver.AddListener(() => cancelRenderer.color = Color.green);
            cancelPassive.OnMouseOut.AddListener(() => cancelRenderer.color = Color.white);
            cancelPassive.OnClick.AddListener(() =>
            {
                if (isObjecting)
                {
                    isObjecting = false;
                    UpdateUI();
                }
            });
            cancelRenderer.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 0.6f);

            GameObject textObj = new GameObject("ObjectorUsesText");
            textObj.transform.SetParent(MeetingHud.Instance.transform, false);
            textObj.transform.localPosition = new Vector3(-3.2f, -1.8f, -5f);  
            usesText = textObj.AddComponent<TextMeshPro>();
            usesText.fontSize = 1.8f;
            usesText.color = Color.white;
            usesText.alignment = TextAlignmentOptions.Left;
         
            UpdateUI();

         
            GameOperatorManager.Instance?.Subscribe<GameUpdateEvent>(_ =>
            {
                bool active = !MyPlayer.IsDead && usesLeft > 0 &&
                    MeetingHud.Instance.CurrentState != MeetingHud.VoteStates.Results;
                if (binder) binder.gameObject.SetActive(active);
                if (usesText) usesText.gameObject.SetActive(active);
            }, this);
        }

        private void UpdateUI()
        {
            if (objectRenderer)
            {
                objectRenderer.color = (!isObjecting && usesLeft > 0) ? Color.white : Color.gray;
                if (objectRenderer.gameObject.TryGetComponent<PassiveButton>(out var btn))
                    btn.enabled = !isObjecting && usesLeft > 0;
            }
            if (cancelRenderer)
            {
                cancelRenderer.color = isObjecting ? Color.white : Color.gray;
                if (cancelRenderer.gameObject.TryGetComponent<PassiveButton>(out var btn))
                    btn.enabled = isObjecting;
            }
            if (usesText != null)
            {
                usesText.text = Language.Translate("objector.ui.uses").Replace("%LEFTUSES%", usesLeft.ToString());
            }
        }

        [Local]
        private void OnFixVote(PlayerFixVoteHostEvent ev)
        {
            if (!isObjecting || !AmOwner || objectionUsedThisMeeting) return;
            ev.VoteTo = null;
            ev.Vote = -2;
        }
        [Local]
        private void OnMeetingEnd(MeetingEndEvent ev)
        {
            if (!AmOwner) return;

            if (isObjecting && ev.Exiled.Count == 0)
            {
                usesLeft--;
                objectionUsedThisMeeting = true;
                new StaticAchievementToken("objector.common.success");
                HsgDebug.Log("achi.suc?");
            }

          
            if (binder) GameObject.Destroy(binder);
            if (usesText) GameObject.Destroy(usesText.gameObject);
            binder = null;
            objectRenderer = null;
            cancelRenderer = null;
            usesText = null;
            isObjecting = false;
        }

    }
}