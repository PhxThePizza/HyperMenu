using UnityEngine;
using Il2CppSystem.Collections.Generic;

namespace MalumMenu;

public class DoorsUI : MonoBehaviour
{
    public static int windowHeight = 270;
    public static int windowWidth = 480;
    private Rect _windowRect;

    private List<SystemTypes> _doorsToSpamOpen = new();
    private List<SystemTypes> _doorsToSpamClose = new();
    private List<SystemTypes> _doorsToKeepClosed = new();
    private System.Collections.Generic.Dictionary<SystemTypes, float> _lastCloseRequest = new System.Collections.Generic.Dictionary<SystemTypes, float>();
    private System.Collections.Generic.Dictionary<int, Vector2> _playerSafePositions = new();

    private void Start()
    {
        // Instantiate 2D area of DoorsUI
        _windowRect = new(
            Screen.width / 2f - windowWidth / 2f,
            Screen.height / 2f - windowHeight / 2f,
            windowWidth,
            windowHeight
        );
    }

    private void OnGUI()
    {
        if (!CheatToggles.showDoorsMenu || !(MenuUI.isGUIActive || MalumMenu.menuKeepSubwindowsOpen.Value) || MalumMenu.isPanicked) return;

        UIHelpers.ApplyUIColor();

        _windowRect = GUI.Window((int)WindowId.DoorsUI, _windowRect, (GUI.WindowFunction)DoorsWindow, "Doors");
    }

    private void DoorsWindow(int windowID)
    {
        if (!Utils.isShip)
        {
            GUI.DragWindow();
            return;
        }

        var map = (MapNames)Utils.GetCurrentMapID();

        if (map is MapNames.MiraHQ)
        {
            GUI.DragWindow();
            return;
        }

        GUILayout.BeginVertical();

        foreach (var doorRoom in DoorsHandler.GetRoomsWithDoors())
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label($"{doorRoom.ToString()}", GUILayout.Width(110f));

            GUILayout.BeginHorizontal();

            GUILayout.Label($"{DoorsHandler.GetStatusOfDoorsInRoom(doorRoom, true)}");

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close", GUIStylePreset.NormalButton, GUILayout.Width(50f)))
            {
                DoorsHandler.CloseDoorsInRoom(doorRoom);
            }

            if (map is MapNames.Polus or MapNames.Airship or MapNames.Fungle)
            {
                if (GUILayout.Button("Open", GUIStylePreset.NormalButton, GUILayout.Width(50f)))
                {
                    DoorsHandler.OpenDoorsInRoom(doorRoom);
                }
            }

            if (Utils.isHost)
            {
                var spamClose = _doorsToSpamClose.Contains(doorRoom);
                spamClose = GUILayout.Toggle(spamClose, "Spam Close", GUIStylePreset.NormalToggle);

                if (spamClose && !_doorsToSpamClose.Contains(doorRoom))
                {
                    _doorsToSpamClose.Add(doorRoom);
                }
                else if (!spamClose && _doorsToSpamClose.Contains(doorRoom))
                {
                    _doorsToSpamClose.Remove(doorRoom);
                }

                if (map is MapNames.Polus or MapNames.Airship or MapNames.Fungle)
                {
                    var spamOpen = _doorsToSpamOpen.Contains(doorRoom);
                    spamOpen = GUILayout.Toggle(spamOpen, "Spam Open", GUIStylePreset.NormalToggle);

                    if (spamOpen && !_doorsToSpamOpen.Contains(doorRoom))
                    {
                        _doorsToSpamOpen.Add(doorRoom);
                    }
                    else if (!spamOpen && _doorsToSpamOpen.Contains(doorRoom))
                    {
                        _doorsToSpamOpen.Remove(doorRoom);
                    }
                }
            }
            else
            {
                // Show keep-closed toggle for non-host clients
                var keepClosed = _doorsToKeepClosed.Contains(doorRoom);
                keepClosed = GUILayout.Toggle(keepClosed, "Keep Closed", GUIStylePreset.NormalToggle, GUILayout.Width(90f));

                if (keepClosed && !_doorsToKeepClosed.Contains(doorRoom))
                {
                    _doorsToKeepClosed.Add(doorRoom);
                }
                else if (!keepClosed && _doorsToKeepClosed.Contains(doorRoom))
                {
                    _doorsToKeepClosed.Remove(doorRoom);
                }

                

                // Clear spam lists if not host
                if (_doorsToSpamClose.Count != 0 || _doorsToSpamOpen.Count != 0)
                {
                    _doorsToSpamClose.Clear();
                    _doorsToSpamOpen.Clear();
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();
        }

        GUILayout.FlexibleSpace();

        GUILayout.Box("", GUIStylePreset.Separator, GUILayout.Height(1f), GUILayout.ExpandWidth(true));
        GUILayout.Space(1f);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Close All", GUIStylePreset.NormalButton))
        {
            CheatToggles.closeAllDoors = true;
        }

        if (map is MapNames.Polus or MapNames.Airship or MapNames.Fungle)
        {
            if (GUILayout.Button("Open All", GUIStylePreset.NormalButton))
            {
                CheatToggles.openAllDoors = true;
            }
        }

        GUILayout.FlexibleSpace();

        if (Utils.isHost)
        {
            CheatToggles.spamCloseAllDoors = GUILayout.Toggle(CheatToggles.spamCloseAllDoors, "Spam Close All", GUIStylePreset.NormalToggle);

            if (map is MapNames.Polus or MapNames.Airship or MapNames.Fungle)
            {
                CheatToggles.spamOpenAllDoors = GUILayout.Toggle(CheatToggles.spamOpenAllDoors, "Spam Open All", GUIStylePreset.NormalToggle);
            }
        }
        else
        {
            CheatToggles.spamCloseAllDoors = CheatToggles.spamOpenAllDoors = false;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUI.DragWindow();
    }

    public void Update()
    {
        if (!Utils.isShip) return;

        // Spam close selected doors
        foreach (var doorRoom in _doorsToSpamClose)
        {
            DoorsHandler.CloseDoorsInRoom(doorRoom);
        }

        // Spam open selected doors
        var map = (MapNames)Utils.GetCurrentMapID();

        if (map is MapNames.Polus or MapNames.Airship or MapNames.Fungle)
        {
            foreach (var doorRoom in _doorsToSpamOpen)
            {
                DoorsHandler.OpenDoorsInRoom(doorRoom);
            }
        }

        // For Skeld/Dleks maps: if keep-closed is enabled for a room, request the host close it every 5 seconds (if enabled)
        if (CheatToggles.autoRequestKeepClosed && (map is MapNames.Skeld or MapNames.Dleks))
        {
            foreach (var doorRoom in _doorsToKeepClosed.ToArray())
            {
                try
                {
                    _lastCloseRequest.TryGetValue(doorRoom, out float last);
                    if (Time.time - last >= 5f)
                    {
                        try { DoorsHandler.CloseDoorsInRoom(doorRoom); } catch { }
                        _lastCloseRequest[doorRoom] = Time.time;
                    }
                }
                catch { }
            }
        }

        // Enforce keep-closed: if any door in the keep list is open, immediately close it locally
        if (_doorsToKeepClosed.Count > 0 && ShipStatus.Instance?.AllDoors != null)
        {
            try
            {
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (!door.IsOpen) continue;

                    if (_doorsToKeepClosed.Contains(door.Room))
                    {
                        try
                        {
                            // Immediately close locally to prevent players walking through
                            door.SetDoorway(false);

                            // Ensure door colliders are enabled and not triggers so they block passage
                            try
                            {
                                var cols = door.GetComponents<Collider2D>();
                                if (cols != null)
                                {
                                    foreach (var c in cols)
                                    {
                                        if (c == null) continue;
                                        c.enabled = true;
                                        c.isTrigger = false;
                                    }
                                }
                                else
                                {
                                    var c = door.GetComponent<Collider2D>();
                                    if (c != null)
                                    {
                                        c.enabled = true;
                                        c.isTrigger = false;
                                    }
                                }
                            }
                            catch { }
                        }
                        catch { }

                        // Also request the server/host to close the doors to keep state in sync
                        try { DoorsHandler.CloseDoorsInRoom(door.Room); } catch { }
                    }
                }
            }
            catch { }
        }
    }

    // Run during the physics step to enforce doors closed before collisions
    public void FixedUpdate()
    {
        if (!Utils.isShip) return;

        if (_doorsToKeepClosed.Count > 0 && ShipStatus.Instance?.AllDoors != null)
        {
            try
            {
                // Track safe positions for all players (positions not inside kept-door colliders)
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.Data == null) continue;
                    Vector2 ppos = player.transform.position;

                    bool insideAny = false;
                    foreach (var d in ShipStatus.Instance.AllDoors)
                    {
                        if (!_doorsToKeepClosed.Contains(d.Room)) continue;

                        try
                        {
                            var cols = d.GetComponents<Collider2D>();
                            if (cols != null)
                            {
                                foreach (var c in cols)
                                {
                                    if (c == null) continue;
                                    if (c.bounds.Contains(ppos)) { insideAny = true; break; }
                                }
                            }
                            else
                            {
                                var c = d.GetComponent<Collider2D>();
                                if (c != null && c.bounds.Contains(ppos)) { insideAny = true; }
                            }
                        }
                        catch { }

                        if (insideAny) break;
                    }

                    if (!insideAny)
                    {
                        _playerSafePositions[player.PlayerId] = ppos;
                    }
                }
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (!door.IsOpen) continue;

                    if (_doorsToKeepClosed.Contains(door.Room))
                    {
                        try
                        {
                            door.SetDoorway(false);

                            // Enable colliders to block players immediately
                            try
                            {
                                var cols = door.GetComponents<Collider2D>();
                                if (cols != null)
                                {
                                    foreach (var c in cols)
                                    {
                                        if (c == null) continue;
                                        c.enabled = true;
                                        c.isTrigger = false;
                                    }
                                }
                                else
                                {
                                    var c = door.GetComponent<Collider2D>();
                                    if (c != null)
                                    {
                                        c.enabled = true;
                                        c.isTrigger = false;
                                    }
                                }
                            }
                            catch { }
                        }
                        catch { }

                        // Also request the server/host to close the doors to keep state in sync
                        try { DoorsHandler.CloseDoorsInRoom(door.Room); } catch { }
                    }
                }
            }
            catch { }
        }

        // If any non-local player is inside a kept-door collider, snap them back to their last safe position
        try
        {
            foreach (var player in PlayerControl.AllPlayerControls)
            {
                if (player == null || player.Data == null) continue;
                if (player == PlayerControl.LocalPlayer) continue;

                Vector2 ppos = player.transform.position;
                bool insideAny = false;

                foreach (var d in ShipStatus.Instance.AllDoors)
                {
                    if (!_doorsToKeepClosed.Contains(d.Room)) continue;
                    try
                    {
                        var cols = d.GetComponents<Collider2D>();
                        if (cols != null)
                        {
                            foreach (var c in cols)
                            {
                                if (c == null) continue;
                                if (c.bounds.Contains(ppos)) { insideAny = true; break; }
                            }
                        }
                        else
                        {
                            var c = d.GetComponent<Collider2D>();
                            if (c != null && c.bounds.Contains(ppos)) { insideAny = true; }
                        }
                    }
                    catch { }

                    if (insideAny) break;
                }

                if (insideAny && _playerSafePositions.TryGetValue(player.PlayerId, out Vector2 safePos))
                {
                    try
                    {
                        // Force local representation back to safe position so they cannot appear to walk through
                        player.transform.position = safePos;

                        // Zero their physics so they can't slide through in the brief open window
                        try
                        {
                            if (player.MyPhysics != null)
                            {
                                player.MyPhysics.Speed = 0f;
                                player.MyPhysics.GhostSpeed = 0f;
                            }

                            var rb = player.GetComponent<Rigidbody2D>();
                            if (rb != null) rb.velocity = Vector2.zero;
                        }
                        catch { }

                        try { player.NetTransform.Halt(); } catch { }
                    }
                    catch { }
                }
            }
        }
        catch { }
    }
}
