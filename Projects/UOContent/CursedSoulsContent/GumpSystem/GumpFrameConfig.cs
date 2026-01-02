using Server;
using Server.Gumps;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.CursedSoulsContent.GumpSystem
{
    // Configurazione del Frame principale
    public class GumpFrameConfig
    {
        public string Title { get; set; } = "Frame";
        public int X { get; set; } = 308;
        public int Y { get; set; } = 35;
        public int Width { get; set; } = 380; // Più largo di default
        public int Height { get; set; } = 468;
        public int ItemsPerPage { get; set; } = 7;

        // Stili
        public int BackgroundArt { get; set; } = 0x13BE;
        public int TitleBackgroundArt { get; set; } = 0xA40;
        public int TitleColor { get; set; } = 0x3B2;
        public int TextColor { get; set; } = 0x480;
        public int ItemBackgroundArt { get; set; } = 0xBBC;

        // Azioni callback
        public Action OnRefresh { get; set; }
        public Action<Mobile> OnAddItem { get; set; }
        public Action<Mobile> OnRemoveAll { get; set; }
    }

    // Configurazione di un elemento della lista
    public class GumpItemConfig
    {
        public object Data { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> InfoLines { get; set; } = new();
        public Dictionary<string, Action<Mobile, object>> Actions { get; set; } = new();
    }

    // Configurazione dei bottoni
    public class GumpButtonConfig
    {
        public string Label { get; set; }
        public int NormalId { get; set; }
        public int PressedId { get; set; }
        public int ButtonIdOffset { get; set; } // Offset per l'ID unico
        public Action<Mobile, object> Action { get; set; } // Action con parametri
        public string Tooltip { get; set; }

        // Posizione relativa all'elemento
        public int XOffset { get; set; } = 0;
        public int YOffset { get; set; } = 8;
    }

    // Configurazione dei bottoni globali del frame
    public class FrameButtonConfig : GumpButtonConfig
    {
        // Posizione assoluta nel frame
        public int X { get; set; }
        public int Y { get; set; }

        public FrameButtonConfig()
        {
            Label = "Refresh";
            NormalId = 0x5FB;
            PressedId = 0x5FC;
            ButtonIdOffset = 500;
            X = new GumpFrameConfig().Width - 30;
            Y = 6;
        }
    }
}
