using FivePD.API;
using static CitizenFX.Core.Native.API;
using Newtonsoft.Json.Linq;
using ScaleformUI.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ScaleformUI;
using CitizenFX.Core;
using ScaleformUI.Elements;
using System.Drawing;
using Newtonsoft.Json;

namespace Client
{
    internal class Client: Plugin
    {
        private UIMenu _serviceMenu;
        private List<string> _servicesAvailableToPlayer;
        private Dictionary<string, List<string>> _currentServiceData;
        private List<UIMenu> _serviceMenus;

        private bool _allowMultiActiveDuty;
        private bool _onDuty;
        private string _activeDutyService;

        private JObject _jsonData;
        internal Client() 
        {
            _jsonData = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "plugins/service_menu/config.json"));
            _servicesAvailableToPlayer = null;
            _currentServiceData = new Dictionary<string, List<string>>();
            _serviceMenus = new List<UIMenu>();
            _allowMultiActiveDuty = bool.Parse(_jsonData["allow-multi-active-duty"].ToString());

            TriggerServerEvent("ServiceMenu:Server:GetPlayerRoles", new Action<string>( async (roles) => 
            {
                await Task.FromResult(0);
                _servicesAvailableToPlayer = JsonConvert.DeserializeObject<List<string>>(roles);

                //Create Menu
                _serviceMenu = BuildServiceMenu();
            }));

            //menu accessibility
            RegisterCommand("servicemenu", new Action(() => { _serviceMenu.Visible = !_serviceMenu.Visible; }), false);
            TriggerEvent("chat:addSuggestion", "/servicemenu", "The Service Menu allows you to see which player services are currently active");
            RegisterKeyMapping("servicemenu", "The Service Menu allows you to see which player services are currently active", "KEYBOARD", "");
        }

        private UIMenu BuildServiceMenu()
        {
            UIMenu menu = new UIMenu("Player Run Services", "Service Status", new PointF(20, 20), new KeyValuePair<string, string>("commonmenu", "interaction_bgd"), false, false, 0.1f);

            //Menu settings
            menu.DescriptionFont = new ItemFont("CharletComprimeColonge", 4);
            menu.SetMouse(true, true, true, true, true);
            menu.BuildingAnimation = MenuBuildingAnimation.LEFT_RIGHT;
            menu.AnimationType = MenuAnimationType.BACK_INOUT;
            menu.ScrollingType = ScrollingType.CLASSIC;

            foreach (var item in _jsonData["services"])
            {
                UIMenuItem menuItem = new UIMenuItem(item["name"].ToString());
                menuItem.SetRightBadge(BadgeIcon.GLOBE_RED);
                menu.AddItem(menuItem);

                _currentServiceData.Add(item["name"].ToString(), new List<string>());

                UIMenu serviceMenu = new UIMenu($"{item["name"]} Units", $"Active Duty {item["name"]} Units", new PointF(20, 20), new KeyValuePair<string, string>("commonmenu", "interaction_bgd"), false, false, 0.1f);
                UIMenuItem serviceMenuItem = new UIMenuItem($"No Active {item["name"]} Units");
                serviceMenu.AddItem(serviceMenuItem);
                _serviceMenus.Add(serviceMenu);

                menuItem.Activated += (sender, selectedItem) =>
                {
                    string msg = "";

                    if (_currentServiceData[menuItem.Label].Count > 0)
                    {
                        msg = $"{menuItem.Label}: ~y~{_currentServiceData[menuItem.Label].Count}~s~ Units On Duty";
                    }
                    else
                    {
                        msg = $"{menuItem.Label}: ~r~0~s~ Units On Duty";
                    }

                    ShowNotification(msg);

                    menu.SwitchTo(serviceMenu);
                };
            }

            //Item for player to go on duty for one of their services
            menu.AddItem(new UIMenuItem("Your Services"));

            //Create Duty Sub-Menu
            UIMenu playerDutyMenu = new UIMenu("Available Player Services", "Services available to you for RP", new PointF(20, 20), new KeyValuePair<string, string>("commonmenu", "interaction_bgd"), false, false, 0.1f);
            if(_servicesAvailableToPlayer != null) 
            { 
                foreach(string item in _servicesAvailableToPlayer) 
                {
                    UIMenuItem menuItem = new UIMenuItem(item, $"Toggle {item} Duty Status");
                    menuItem.ItemData = false;
                    playerDutyMenu.AddItem(menuItem);

                    menuItem.Activated += (sender, selectedItem) =>
                    {
                        if (!_allowMultiActiveDuty && _onDuty)
                        {
                            if(selectedItem.Label == _activeDutyService)
                            {
                                selectedItem.ItemData = !(bool)selectedItem.ItemData;
                                TriggerServerEvent("ServiceStatus:Server:PlayerToggleDutyStatus", menuItem.Label, selectedItem.ItemData);
                                if ((bool)selectedItem.ItemData)
                                {
                                    selectedItem.SetRightBadge(BadgeIcon.TICK);
                                    _activeDutyService = selectedItem.Label;
                                    _onDuty = true;
                                }
                                else
                                {
                                    selectedItem.SetRightBadge(BadgeIcon.NONE);
                                    _activeDutyService = "";
                                    _onDuty = false;
                                }
                            }
                            else
                            {
                                ShowNotification("You're already On Duty as: " + _activeDutyService);
                            }
                        }
                        else if (!_allowMultiActiveDuty && !_onDuty)
                        {
                            selectedItem.ItemData = !(bool)selectedItem.ItemData;
                            TriggerServerEvent("ServiceStatus:Server:PlayerToggleDutyStatus", menuItem.Label, selectedItem.ItemData);
                            if ((bool)selectedItem.ItemData)
                            {
                                selectedItem.SetRightBadge(BadgeIcon.TICK);
                                _activeDutyService = selectedItem.Label;
                                _onDuty = true;
                            }
                            else
                            {
                                selectedItem.SetRightBadge(BadgeIcon.NONE);
                                _activeDutyService = "";
                                _onDuty = false;
                            }
                        }
                        else if(_allowMultiActiveDuty)
                        {
                            selectedItem.ItemData = !(bool)selectedItem.ItemData;
                            TriggerServerEvent("ServiceStatus:Server:PlayerToggleDutyStatus", menuItem.Label, selectedItem.ItemData);
                            if ((bool)selectedItem.ItemData)
                            {
                                selectedItem.SetRightBadge(BadgeIcon.TICK);
                            }
                            else
                            {
                                selectedItem.SetRightBadge(BadgeIcon.NONE);
                            }
                        }
                    };
                }
            }
            else
            {
                UIMenuItem menuItem = new UIMenuItem("No Menu Items");
                playerDutyMenu.AddItem(menuItem);
            }

            //Player has selected Go On Duty
            menu.MenuItems.Last().Activated += (sender, args) =>
            {
                sender.SwitchTo(playerDutyMenu);
            };

            return menu;
        }

        [EventHandler("ServiceStatus:Client:ServicesUpdated")]
        private void HandleServiceStatusUpdate1(string data) 
        {
            Dictionary<string, List<string>> serviceData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(data);

            foreach(var item in serviceData) 
            { 
                if( item.Value.Count > 0) 
                {
                    if (_currentServiceData[item.Key].Count == 0)
                    {
                        ShowNotification($"{item.Key} ~g~On Duty~s~");
                    }

                    UIMenuItem menuItem = _serviceMenu.MenuItems.Where(i => i.Label == item.Key).FirstOrDefault();
                    menuItem.SetRightBadge(BadgeIcon.GLOBE_GREEN);

                    UIMenu serviceMenu = null;
                    //get related submenu
                    foreach(UIMenu menu in _serviceMenus)
                    {
                        if (menu.Title == $"{item.Key} Units")
                        {
                            serviceMenu = menu;
                            break;
                        }
                    }

                    if(serviceMenu != null)
                    {
                        serviceMenu.Clear();

                        foreach(string name in item.Value)
                        {
                            UIMenuItem newMenuItem = new UIMenuItem(name);
                            serviceMenu.AddItem(newMenuItem);
                        }
                    }

                    serviceMenu.RefreshMenu(false);
                }
                else if(item.Value.Count == 0) 
                {
                    if (_currentServiceData[item.Key].Count > 0)
                    {
                        ShowNotification($"{item.Key} ~r~Off Duty~s~");
                    }

                    UIMenuItem menuItem = _serviceMenu.MenuItems.Where(i => i.Label == item.Key).FirstOrDefault();
                    menuItem.SetRightBadge(BadgeIcon.GLOBE_RED);

                    UIMenu serviceMenu = null;
                    foreach (UIMenu menu in _serviceMenus)
                    {
                        if(menu.Title == $"{item.Key} Units")
                        {
                            serviceMenu = menu;
                            break;
                        }
                    }

                    if (serviceMenu != null) 
                    {
                        serviceMenu.Clear();
                        serviceMenu.AddItem(new UIMenuItem($"No Active {item.Key} Units"));
                    }

                    serviceMenu.RefreshMenu(false);
                }
            }

            _currentServiceData = serviceData;
        }
        private void ShowNotification(string message)
        {
            BeginTextCommandThefeedPost("STRING");
            AddTextComponentString(message);
            EndTextCommandThefeedPostMessagetext("CHAR_CALL911", "CHAR_CALL911", false, 4, "~y~Service Status", "~b~Update");
            EndTextCommandThefeedPostTicker(false, false);
        }
    }
}
