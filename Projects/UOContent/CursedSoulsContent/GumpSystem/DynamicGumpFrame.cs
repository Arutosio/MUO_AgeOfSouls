using System;
using System.Text;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using System.Linq;
using Server.Mobiles;

namespace Server.CursedSoulsContent.GumpSystem
{
    public class DynamicGumpFrame
    {
        private readonly Gump _parentGump;
        private readonly GumpFrameConfig _config;
        private readonly List<GumpItemConfig> _items;
        private readonly List<GumpButtonConfig> _itemButtons;
        private readonly List<FrameButtonConfig> _frameButtons;
        private int _currentPage;

        // ID base per i bottoni (per evitare conflitti)
        private int BASE_BUTTON_ID = 1000;
        private int BASE_BUTTON_ID_Refresh = 999;
        private int BASE_ITEM_BUTTON_ID = 2000;
        private int BASE_PAGE_BUTTON_ID_Prev = 3000;
        private int BASE_PAGE_BUTTON_ID_Next = 3001;

        public int CurrentPage
        {
            get { return _currentPage; }
            set { _currentPage = Math.Max(0, value); }
        }


        public DynamicGumpFrame(Gump parentGump, GumpFrameConfig config,
                               List<GumpItemConfig> items,
                               List<GumpButtonConfig> itemButtons = null,
                               List<FrameButtonConfig> frameButtons = null,
                               int currentPage = 0, int idStartButtonItem = 2000,
                               int idPrevButton = 3000, int idNextButton = 3001,
                               int idRefreshButton = 999)
        {
            _parentGump = parentGump;
            _config = config;
            BASE_ITEM_BUTTON_ID = idStartButtonItem;
            BASE_BUTTON_ID_Refresh = idRefreshButton;
            BASE_PAGE_BUTTON_ID_Prev = idPrevButton;
            BASE_PAGE_BUTTON_ID_Next = idNextButton;
            _items = items ?? new List<GumpItemConfig>();
            _itemButtons = itemButtons ?? GetDefaultItemButtons();
            _frameButtons = frameButtons ?? GetDefaultFrameButtons();
            _currentPage = Math.Max(0, currentPage);
        }

        // Bottoni di default per gli elementi
        private List<GumpButtonConfig> GetDefaultItemButtons()
        {
            var retListGumpButtonConfig = new List<GumpButtonConfig>();

            var tmpGumpButtonConfig = new GumpButtonConfig();
            tmpGumpButtonConfig.Label = "Rename";
            tmpGumpButtonConfig.NormalId = 0xFAB; // Pulsante "Edit"
            tmpGumpButtonConfig.PressedId = 0xFAD;
            tmpGumpButtonConfig.ButtonIdOffset = 0;
            tmpGumpButtonConfig.XOffset = 0;
            retListGumpButtonConfig.Add(tmpGumpButtonConfig);

            tmpGumpButtonConfig = new GumpButtonConfig();
            tmpGumpButtonConfig.Label = "Move";
            tmpGumpButtonConfig.NormalId = 0x13E0; // Icona movimento
            tmpGumpButtonConfig.PressedId = 0x13E1;
            tmpGumpButtonConfig.ButtonIdOffset = 1000;
            tmpGumpButtonConfig.XOffset = 4;
            retListGumpButtonConfig.Add(tmpGumpButtonConfig);

            tmpGumpButtonConfig = new GumpButtonConfig();
            tmpGumpButtonConfig.Label = "Delete";
            tmpGumpButtonConfig.NormalId = 0xFB1; // Icona "X"
            tmpGumpButtonConfig.PressedId = 0xFB3;
            tmpGumpButtonConfig.ButtonIdOffset = 2000;
            tmpGumpButtonConfig.XOffset = 80;
            retListGumpButtonConfig.Add(tmpGumpButtonConfig);

            return retListGumpButtonConfig;
        }

        // Bottoni di default per il frame
        private List<FrameButtonConfig> GetDefaultFrameButtons()
        {
            var retListFrameButtonConfig = new List<FrameButtonConfig>();

            var tmpFrameButtonConfig = new FrameButtonConfig();
            tmpFrameButtonConfig.X = _config.Width - 30;
            tmpFrameButtonConfig.Y = 6;
            tmpFrameButtonConfig.ButtonIdOffset = BASE_BUTTON_ID_Refresh;
            retListFrameButtonConfig.Add(tmpFrameButtonConfig);

            return retListFrameButtonConfig;
        }

        // Metodo principale per renderizzare il frame
        public void Render()
        {
            int frameX = _config.X;
            int frameY = _config.Y;
            int frameWidth = _config.Width;
            int frameHeight = _config.Height;

            // Background del frame
            _parentGump.AddBackground(frameX, frameY, frameWidth, frameHeight, _config.BackgroundArt);
            _parentGump.AddImageTiled(frameX + 5, frameY + 5, frameWidth - 10, 25, _config.TitleBackgroundArt);
            _parentGump.AddAlphaRegion(frameX + 5, frameY + 5, frameWidth - 10, frameHeight - 10);

            // Titolo
            _parentGump.AddLabel(frameX + 15, frameY + 10, _config.TitleColor, _config.Title);

            // Calcola paginazione
            int totalPages = (int)Math.Ceiling(_items.Count / (double)_config.ItemsPerPage);
            totalPages = Math.Max(1, totalPages);

            // Assicurati che la pagina corrente sia valida
            _currentPage = Math.Max(0, Math.Min(_currentPage, totalPages - 1));

            int startIndex = _currentPage * _config.ItemsPerPage;
            int endIndex = Math.Min(startIndex + _config.ItemsPerPage, _items.Count);



            // Informazioni paginazione
            _parentGump.AddLabel(frameX + 15, frameY + 35, _config.TextColor,
                               $"Items: {_items.Count} (Page {_currentPage + 1}/{totalPages})");

            // Bottoni globali del frame
            RenderFrameButtons(frameX, frameY);

            // Renderizza elementi della pagina corrente
            RenderItems(frameX, frameY, frameWidth, startIndex, endIndex);

            // Paginazione
            if (totalPages > 1)
            {
                RenderPagination(frameX, frameY, frameWidth, frameHeight, totalPages);
            }
        }

        private void RenderFrameButtons(int frameX, int frameY)
        {
            foreach (var btn in _frameButtons)
            {
                int buttonId = BASE_BUTTON_ID + btn.ButtonIdOffset;
                _parentGump.AddButton(frameX + btn.X, frameY + btn.Y,
                                    btn.NormalId, btn.PressedId, buttonId, GumpButtonType.Reply, 0);

                if (!string.IsNullOrEmpty(btn.Label))
                {
                    _parentGump.AddLabel(frameX + btn.X - 50, frameY + btn.Y,
                                       _config.TextColor, btn.Label);
                }
            }
        }

        private void RenderItems(int frameX, int frameY, int frameWidth,
                                int startIndex, int endIndex)
        {
            int itemStartY = frameY + 58;
            int itemHeight = 54;

            for (int i = startIndex; i < endIndex; i++)
            {
                var item = _items[i];
                int lineY = itemStartY + ((i - startIndex) * itemHeight);

                // Background dell'elemento
                _parentGump.AddImageTiled(frameX + 10, lineY, frameWidth - 20, itemHeight - 5,
                                        _config.ItemBackgroundArt);

                // Titolo dell'elemento
                _parentGump.AddLabel(frameX + 15, lineY, _config.TextColor, item.Title);

                // Descrizione
                if (!string.IsNullOrEmpty(item.Description))
                {
                    _parentGump.AddLabel(frameX + 15, lineY + 15, _config.TitleColor, item.Description);
                }

                // Informazioni aggiuntive
                int infoY = lineY + 30;
                foreach (var info in item.InfoLines.Take(2))
                {
                    _parentGump.AddLabel(frameX + 15, infoY, 0x3B2, info);
                    infoY += 12;
                }

                // Bottoni per l'elemento
                //RenderItemButtons(frameX, frameWidth, lineY, i, item);
                RenderItemButtons(frameX, frameWidth, lineY, i);
            }
        }

        private void RenderItemButtons(int frameX, int frameWidth, int lineY, int itemIndex)
        {
            int buttonX = frameX + frameWidth - 125; // Parti da destra
            int buttonY = lineY - 6;

            foreach (var btn in _itemButtons)
            {
                // Calcola ID unico per il bottone (itemIndex * 100 + offset)
                //int buttonId = BASE_ITEM_BUTTON_ID + (itemIndex * 100) + btn.ButtonIdOffset;
                int buttonId = BASE_ITEM_BUTTON_ID + itemIndex + btn.ButtonIdOffset;

                _parentGump.AddButton(buttonX + btn.XOffset, buttonY + btn.YOffset,
                                    btn.NormalId, btn.PressedId, buttonId, GumpButtonType.Reply, 0);

                if (!string.IsNullOrEmpty(btn.Label))
                {
                    int labelY = buttonY + 30;
                    _parentGump.AddLabel(buttonX + btn.XOffset - 5, labelY,
                                       _config.TitleColor, btn.Label);
                }

                // Sposta per il prossimo bottone (Spaziatura)
                buttonX -= 5;
            }
        }

        private void RenderPagination(int frameX, int frameY, int frameWidth, int frameHeight, int totalPages)
        {
            int paginationX = frameX + frameWidth - 15;
            int paginationY = frameY + 32;

            if (_currentPage > 0)
            {
                _parentGump.AddButton(paginationX - 90, paginationY, 0xFAE, 0xFAF,
                                    BASE_PAGE_BUTTON_ID_Prev, GumpButtonType.Reply, 0);
                _parentGump.AddLabel(paginationX - 120, paginationY, _config.TextColor, "Prev");
            }

            if (_currentPage < totalPages - 1)
            {
                _parentGump.AddButton(paginationX - 60, paginationY, 0xFA5, 0xFA7,
                                    BASE_PAGE_BUTTON_ID_Next, GumpButtonType.Reply, 0);
                _parentGump.AddLabel(paginationX - 26, paginationY, _config.TextColor, "Next");
            }
        }

        // Metodo per gestire i click (da chiamare nel OnResponse del Gump padre)
        public bool HandleResponse(Mobile from, int buttonId, object customData = null)
        {
            // Gestione paginazione
            if (buttonId == BASE_PAGE_BUTTON_ID_Prev) // Prev
            {
                _currentPage = Math.Max(0, _currentPage - 1);
                RefreshGump(from);
                return true;
            }
            else if (buttonId == BASE_PAGE_BUTTON_ID_Next) // Next
            {
                _currentPage++;
                RefreshGump(from);
                return true;
            }

            // Gestione bottoni globali del frame
            foreach (var btn in _frameButtons)
            {
                if (buttonId == BASE_BUTTON_ID + btn.ButtonIdOffset)
                {
                    btn.Action?.Invoke(from, customData);
                    return true;
                }
            }

            // Gestione bottoni degli elementi
            if (buttonId >= BASE_ITEM_BUTTON_ID && buttonId < BASE_PAGE_BUTTON_ID_Prev)
            {
                int relativeId = buttonId - BASE_ITEM_BUTTON_ID;
                int itemIndex = relativeId / 100;
                int buttonOffset = relativeId % 100;

                if (itemIndex >= 0 && itemIndex < _items.Count)
                {
                    var item = _items[itemIndex];

                    // Trova il bottone corrispondente
                    var btn = _itemButtons.FirstOrDefault(b => b.ButtonIdOffset == buttonOffset);
                    if (btn != null)
                    {
                        btn.Action?.Invoke(from, item.Data);
                        return true;
                    }
                }
            }

            return false;
        }

        // Metodo helper per refreshare il Gump mantenendo la pagina corrente
        private void RefreshGump(Mobile from)
        {
            // Chiama l'action di refresh passando la pagina corrente come parte dello stato
            if (_config.OnRefresh != null)
            {
                // Se l'action è un delegate, chiamalo
                _config.OnRefresh.Invoke();
            }
            else
            {
                // Altrimenti, forza il refresh manualmente
                from.SendGump(new GuildTerritoryManagementGump(
                    GetStoneFromConfig(),
                    from as PlayerMobile,
                    GetPageFromConfig(),
                    _currentPage));
            }
        }

        // Metodi helper per estrarre informazioni dalla configurazione
        private GuildTerritoryStone GetStoneFromConfig()
        {
            // Questi metodi dipendono dalla tua implementazione
            // Potresti dover salvare lo stone nella configurazione
            return null; // Implementa secondo le tue esigenze
        }

        private int GetPageFromConfig()
        {
            // Ritorna la pagina del Gump principale
            return 1; // Per Security page, 2 per Vendors page, etc.
        }
    }
}
