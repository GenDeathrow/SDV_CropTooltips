using StardewModdingAPI;

namespace CropToolTip
{
    class ConfigOptions
    {
        public bool Show_On_Mouse_Hover_Default { get; set; } = true;

        public bool Show_Harvest_Time_Left { get; set; } = true;

        public bool Show_If_Plant_Will_Die_Before_Harvest { get; set; } = true;

        public SButton Toggle_Keybind { get; set; } = SButton.O;
    }
}
