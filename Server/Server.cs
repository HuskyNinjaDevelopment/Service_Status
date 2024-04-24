using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Server
{
    internal class Server: BaseScript
    {
        private bool _usingAcePerms;
        private bool _usingDiscordRoles;

        private Dictionary<string, List<string>> _services;

        private JObject _jsonData;

        public Server() 
        {
            _jsonData = JObject.Parse(LoadResourceFile(GetCurrentResourceName(), "config.json")); //load config file
            _usingDiscordRoles = bool.Parse(_jsonData["use-discord-roles"].ToString());
            _usingAcePerms = bool.Parse(_jsonData["use-ace-perms"].ToString());

            if(_usingDiscordRoles && _usingAcePerms ) 
            {
                //No point to do both
                //Ace Perms is quicker/easier go with that if its set up
                _usingDiscordRoles = false;
            }

            _services = new Dictionary<string, List<string>>();

            //populate services
            foreach(var item in _jsonData["services"])
            {
                _services.Add(item["name"].ToString(), new List<string>());
            }
        }

        [EventHandler("playerDropped")]
        private void HandlePlayerDisconnect([FromSource] Player source, string reason)
        {
            if(_services != null)
            {
               foreach(var item in _services)
                {
                    if(item.Value.Contains(source.Name))
                    {
                        item.Value.Remove(source.Name);
                        TriggerClientEvent("ServiceStatus:Client:ServicesUpdated", JsonConvert.SerializeObject(_services));
                        break;
                    }
                }
            }
        }

        [EventHandler("ServiceMenu:Server:GetPlayerRoles")]
        private async void HandleGetPlayerRoles([FromSource]Player source, NetworkCallbackDelegate callback)
        {
            List<string> discordRoles= new List<string>();
            List<string> acePerms = new List<string>();

            List<Object> discordPlayerRoles = null;
            if(_usingDiscordRoles)
            {
                while (Exports["Badger_Discord_API"].GetDiscordRoles(source.Handle).GetType() != typeof(List<Object>)) { await Delay(500); }
                discordPlayerRoles = Exports["Badger_Discord_API"].GetDiscordRoles(source.Handle);
            }

            List<string> roles = new List<string>();

            foreach(var item in _jsonData["services"])
            {
                if(_usingAcePerms)
                {
                    if (IsPlayerAceAllowed(source.Handle, item["ace-perm"].ToString()))
                    {
                        roles.Add(item["name"].ToString());
                    }
                }

                if(_usingDiscordRoles)
                {
                    while (Exports["Badger_Discord_API"].GetRoleIdFromRoleName(item["discord-role"].ToString()).GetType() != typeof(ulong)) { await Delay(500); }
                    ulong roleId = Exports["Badger_Discord_API"].GetRoleIdFromRoleName(item["discord-role"].ToString());

                    try
                    {
                        foreach(var role in discordPlayerRoles)
                        {
                            if(role.GetType() == typeof(String) && ulong.Parse(role.ToString()) == roleId)
                            {
                                roles.Add(item["name"].ToString());
                                break;
                            }
                        }
                    }
                    catch { /*Silent Fail*/ }
                }
            }

            _ = callback.Invoke(JsonConvert.SerializeObject(roles));
        }

        [EventHandler("ServiceStatus:Server:PlayerToggleDutyStatus")]
        private void HandlePlayerToggleDutyStatus([FromSource]Player source, string service, bool status)
        {
            if(status)
            {
                if (!_services[service].Contains(source.Name))
                {
                    _services[service].Add(source.Name);
                }
            }
            else 
            {
                if (_services[service].Contains(source.Name))
                {
                    _services[service].Remove(source.Name);
                }
            }

            TriggerClientEvent("ServiceStatus:Client:ServicesUpdated", JsonConvert.SerializeObject(_services));
        }
    }
}
