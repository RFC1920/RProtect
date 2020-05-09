using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Research Protection", "RFC1920", "0.1.0")]
    class RProtect : RustPlugin
    {
        private const string RPGUI = "blueblocker.gui";
        private const string RPGUI2 = "blueblocker.gui2";
        private Dictionary<uint, ulong> rsloot = new Dictionary<uint, ulong>();
        private Dictionary<uint, CuiLabel> rslabel = new Dictionary<uint, CuiLabel>();
        private List<ulong> canres = new List<ulong>();

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Message(Lang(key, player.Id, args));
        #endregion

        void Init()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["alldone"] = "You have already researched this item!",
                ["override"] = "Click again to override -->",
                ["proton"] = "Research protection enabled"
            }, this);
        }

        private object CanResearchItem(BasePlayer player, Item item)
        {
            if (player == null) return null;
            if (item == null) return null;
            if(player.blueprints.HasUnlocked(item.info))
            {
                if (canres.Contains(player.userID)) return null;

                if (rsloot.ContainsValue(player.userID))
                {
                    var rst = rsloot.FirstOrDefault(x => x.Value == player.userID).Key;
                    canres.Add(player.userID);
                    RsGUI(player, rst, Lang("alldone"));
                    timer.Once(3f, () => RsGUI(player, rst));
                    timer.Once(3f, () => canres.Remove(player.userID));
                }
                return false;
            }
            return null;
        }

        void Unload()
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, RPGUI);
                CuiHelper.DestroyUi(player, RPGUI2);
            }
        }

        private object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            var rst = container.GetComponentInParent<ResearchTable>() ?? null;
            if(rst == null) return null;

            if(rsloot.ContainsKey(rst.net.ID)) return null;

            rsloot.Add(rst.net.ID, player.userID);
            RsGUI(player, rst.net.ID);

            return null;
        }

        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if(!rsloot.ContainsKey(entity.net.ID)) return;
            if(entity == null) return;

            if(rsloot[entity.net.ID] == player.userID)
            {
                CuiHelper.DestroyUi(player, RPGUI);
                CuiHelper.DestroyUi(player, RPGUI2);
                rsloot.Remove(entity.net.ID);
            }
        }

        void RsGUI(BasePlayer player, uint rst, string label = null)
        {
            CuiHelper.DestroyUi(player, RPGUI);
            CuiHelper.DestroyUi(player, RPGUI2);

            CuiElementContainer container = UI.Container(RPGUI, UI.Color("444444", 1f), "0.77 0.798", "0.9465 0.835", true, "Overlay");
            string uicolor = "#ff3333";
            if(label == null)
            {
                label = Lang("proton");
                uicolor = "#dddddd";
            }
            string lname = "label" + rst.ToString();
            UI.Label(ref container, RPGUI, UI.Color(uicolor, 1f), label, 12, "0 0", "1 1", lname);

            CuiHelper.AddUi(player, container);

            if (canres.Contains(player.userID))
            {
                CuiElementContainer cont2 = UI.Container(RPGUI2, UI.Color("ff4444", 1f), "0.66 0.163", "0.765 0.205", true, "Overlay");
                UI.Label(ref cont2, RPGUI2, UI.Color("#ffffff", 1f), Lang("override"), 12, "0 0", "1 1", "none");
                CuiHelper.AddUi(player, cont2);
            }

        }

        #region Classes
        public static class UI
        {
            public static CuiElementContainer Container(string panel, string color, string min, string max, bool useCursor = false, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = color },
                            RectTransform = {AnchorMin = min, AnchorMax = max},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
                return container;
            }
            public static void Label(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, string labelname, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = min, AnchorMax = max }
                },
                panel, labelname);
            }
            public static string Color(string hexColor, float alpha)
            {
                if(hexColor.StartsWith("#"))
                {
                    hexColor = hexColor.Substring(1);
                }
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion
    }
}
