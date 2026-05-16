using System.Collections.Generic;
using UnityEngine;
using Sentry.Internal.Extensions;

namespace MalumMenu;
public static class MalumESP
{
    private static bool _freecamActive;
    private static bool _resolutionChangeNeeded;
    public static void SporeCloudVision(Mushroom mushroom)
    {
        if (CheatToggles.noShadows)
        {
            // Change the Z axis position of spore clouds as to make players appear above them

            mushroom.sporeMask.transform.position = new Vector3(mushroom.sporeMask.transform.position.x, mushroom.sporeMask.transform.position.y, -1);
            return;
        }

        // Normal Z axis position: 5f
        mushroom.sporeMask.transform.position = new Vector3(mushroom.sporeMask.transform.position.x, mushroom.sporeMask.transform.position.y, 5f);
    }

    public static bool IsFullbrightActive()
    {
        // Fullbright is automatically activated when zooming out, spectating other players, or "freecamming"
        // This is done to avoid issues with shadows

        return CheatToggles.noShadows || Camera.main.orthographicSize > 3f || Camera.main.gameObject.GetComponent<FollowerCamera>().Target != PlayerControl.LocalPlayer;
    }

    public static void ZoomOut(HudManager hudManager)
    {
        if (CheatToggles.zoomOut)
        {
            if (hudManager.Chat.IsOpenOrOpening || PlayerCustomizationMenu.Instance || (Utils.isLobby && (FriendsListUI.Instance.IsOpen ||
                GameStartManager.Instance.LobbyInfoPane.LobbyViewSettingsPane.gameObject.active || GameStartManager.Instance.RulesEditPanel))) return;

            _resolutionChangeNeeded = true;

            if (Input.GetAxis("Mouse ScrollWheel") < 0f ) // Zoom out
            {

                // Both the main camera and the UI camera need to be adjusted

                Camera.main.orthographicSize++;
                hudManager.UICamera.orthographicSize++;

                // Utils.AdjustResolution() seems to be needed to properly sync the game's UI
                // after a change in orthographicSize

                Utils.AdjustResolution();

            }
            else if (Input.GetAxis("Mouse ScrollWheel") > 0f )
            {
                // Zoom in
                if (!(Camera.main.orthographicSize > 3f)) return; // Never go below the default orthographicSize: 3f

                Camera.main.orthographicSize--;
                hudManager.UICamera.orthographicSize--;

                Utils.AdjustResolution();
            }
        }
        else
        {
            // orthographicSize is reset to default value: 3f
            Camera.main.orthographicSize = 3f;
            hudManager.UICamera.orthographicSize = 3f;

            // Utils.AdjustResolution() is invoked one last time to prevent issues with UI
            if (_resolutionChangeNeeded)
            {
                Utils.AdjustResolution();
                _resolutionChangeNeeded = false;
            }
        }
    }

    public static void MeetingNametags(MeetingHud meetingHud)
    {
        try
        {
            foreach (var playerState in meetingHud.playerStates)
            {
                // Fetch the NetworkedPlayerInfo of each playerState
                var data = GameData.Instance.GetPlayerById(playerState.TargetPlayerId);

                if (data.IsNull() || data.Disconnected || data.Outfits[PlayerOutfitType.Default].IsNull()) continue;

                // Update the player's nametag appropriately
                playerState.NameText.text = Utils.GetNameTag(data, data.DefaultOutfit.PlayerName);

                // Move and resize the nametag to prevent it overlapping with colorblind text
                if (CheatToggles.seeRoles && CheatToggles.seePlayerInfo)
                {
                    playerState.NameText.transform.localPosition = new Vector3(0.33f, 0.08f, 0f);
                    playerState.NameText.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                }
                else if (CheatToggles.seeRoles || CheatToggles.seePlayerInfo)
                {
                    playerState.NameText.transform.localPosition = new Vector3(0.3384f, 0.1125f, -0.1f);
                    playerState.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f);
                }
                else
                {
                    // Reset the position and scale of the nametag to default values (they're kinda weird but whatever)
                    playerState.NameText.transform.localPosition = new Vector3(0.3384f, 0.0311f, -0.1f);
                    playerState.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f);
                }
            }
        } catch { }
    }

    public static void PlayerNametags(PlayerPhysics playerPhysics)
    {
        try
        {
            if (playerPhysics.myPlayer.cosmetics != null)
            {
                playerPhysics.myPlayer.cosmetics.SetName(Utils.GetNameTag(playerPhysics.myPlayer.Data, playerPhysics.myPlayer.CurrentOutfit.PlayerName));
            }

            float ventOpacity = Mathf.Clamp(CheatToggles.ventPlayerOpacity, 0f, 100f) / 100f;

            if (!playerPhysics.myPlayer.inVent || ventOpacity <= 0f)
            {
                if (!playerPhysics.myPlayer.inVent && !playerPhysics.myPlayer.Data.IsDead)
                {
                    RestoreVentAppearance(playerPhysics.myPlayer);

                    if (playerPhysics.myPlayer.cosmetics != null && playerPhysics.myPlayer.cosmetics.nameText != null)
                    {
                        if (CheatToggles.seeRoles && CheatToggles.seePlayerInfo)
                        {
                            playerPhysics.myPlayer.cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.186f, 0f);
                        }
                        else if (CheatToggles.seeRoles || CheatToggles.seePlayerInfo)
                        {
                            playerPhysics.myPlayer.cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.093f, 0f);
                        }
                        else
                        {
                            playerPhysics.myPlayer.cosmetics.nameText.transform.localPosition = new Vector3(0f, 0f, 0f);
                        }
                    }
                }

                return;
            }

            // Show with opacity
            if (playerPhysics.myPlayer.Collider != null) playerPhysics.myPlayer.Collider.enabled = false;

            if (playerPhysics.myPlayer.cosmetics != null)
            {
                playerPhysics.myPlayer.cosmetics.gameObject.SetActive(true);
            }

            // Apply opacity to all renderers so hats, pets, and body cosmetics stay visible too.
            ApplyVentOpacity(playerPhysics.myPlayer, ventOpacity);

            // Show nametag
            if (playerPhysics.myPlayer.cosmetics?.nameText != null)
            {
                playerPhysics.myPlayer.cosmetics.nameText.gameObject.SetActive(true);
                playerPhysics.myPlayer.cosmetics.nameText.enabled = true;
            }

            if (playerPhysics.myPlayer.cosmetics?.colorBlindText != null)
            {
                playerPhysics.myPlayer.cosmetics.colorBlindText.gameObject.SetActive(true);
                playerPhysics.myPlayer.cosmetics.colorBlindText.enabled = true;
            }
        } catch { }
    }

    private static void RestoreVentAppearance(PlayerControl player)
    {
        if (player == null) return;

        if (player.Collider != null) player.Collider.enabled = true;

        if (player.cosmetics != null)
        {
            player.cosmetics.gameObject.SetActive(true);
        }

        foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            // Make sure the renderer's GameObject is active so enabling the renderer actually displays it
            try { renderer.gameObject.SetActive(true); } catch { }

            renderer.enabled = true;

            if (renderer is SpriteRenderer spriteRenderer)
            {
                if (!Mathf.Approximately(spriteRenderer.color.a, 1f))
                {
                    spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1f);
                }
            }
            else if (renderer.material != null && renderer.material.HasProperty("_Color"))
            {
                var color = renderer.material.color;
                if (!Mathf.Approximately(color.a, 1f))
                {
                    color.a = 1f;
                    renderer.material.color = color;
                }
            }
        }

        if (player.cosmetics?.nameText != null)
        {
            player.cosmetics.nameText.gameObject.SetActive(true);
            player.cosmetics.nameText.enabled = true;
        }

        if (player.cosmetics?.colorBlindText != null)
        {
            player.cosmetics.colorBlindText.gameObject.SetActive(true);
            player.cosmetics.colorBlindText.enabled = true;
        }
    }

    private static void ApplyVentOpacity(PlayerControl player, float opacity)
    {
        if (player == null) return;

        foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
        {
            var name = renderer.gameObject.name;
            if (name.IndexOf("arrow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("task", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                renderer.enabled = true;
                continue;
            }

            // Ensure the renderer's GameObject is active so hats/pets (which may be disabled) become visible
            try { renderer.gameObject.SetActive(true); } catch { }

            renderer.enabled = true;

            if (renderer is SpriteRenderer spriteRenderer)
            {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, opacity);
            }
            else if (renderer.material != null && renderer.material.HasProperty("_Color"))
            {
                var color = renderer.material.color;
                color.a = opacity;
                renderer.material.color = color;
            }
        }
    }

    public static void ChatNametags(ChatBubble chatBubble)
    {
        try
        {
            // Update the player's nametag appropriately
            chatBubble.NameText.text = Utils.GetNameTag(chatBubble.playerInfo, chatBubble.NameText.text, true);

            // Adjust the chatBubble's size to the new nametag to prevent issues
            chatBubble.NameText.ForceMeshUpdate(true, true);
            chatBubble.Background.size = new Vector2(5.52f, 0.2f + chatBubble.NameText.GetNotDumbRenderedHeight() + chatBubble.TextArea.GetNotDumbRenderedHeight());
            chatBubble.MaskArea.size = chatBubble.Background.size - new Vector2(0f, 0.03f);

        } catch { }
    }

    public static void SeeGhostsCheat(PlayerPhysics playerPhysics)
    {
        try{

            if(playerPhysics.myPlayer.Data.IsDead && !PlayerControl.LocalPlayer.Data.IsDead)
            {
                playerPhysics.myPlayer.Visible = CheatToggles.seeGhosts;
            }

        }catch{}
    }

    public static void FreecamCheat()
    {
        if (CheatToggles.freecam)
        {
            // Completely disable FollowerCamera
            if (!_freecamActive)
            {

                Camera.main.gameObject.GetComponent<FollowerCamera>().enabled = false;
                Camera.main.gameObject.GetComponent<FollowerCamera>().Target = null;

                _freecamActive = true;

            }

            // Prevent the player from moving while in freecam
            PlayerControl.LocalPlayer.moveable = false;

            // Get keyboard input
            var movement = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.0f);

            // Change the camera's position depending on the keyboard input
            // Speed: 10f
            Camera.main.transform.position = Camera.main.transform.position + movement * 10f * Time.deltaTime;

        }
        else
        {
            // Re-enable FollowerCamera & movement once freecam is disabled
            if (!_freecamActive) return;
            PlayerControl.LocalPlayer.moveable = true;
            Camera.main.gameObject.GetComponent<FollowerCamera>().enabled = true;
            Camera.main.gameObject.GetComponent<FollowerCamera>().SetTarget(PlayerControl.LocalPlayer);
            _freecamActive = false;
        }
    }
}
