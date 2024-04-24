# Installaion
There are two components to get everything working as intended, the FivePD Plugin and the Service_Status Resource.

**FivePD**: To install the FivePD Plugin, place the service_menu folder inside your fivepd/plugins folder.\
**Service_Status**: Place the service_status folder inside your resource folder.

**server.cfg**: Be sure to start the service_status resource after you start Badger_Discord_API and/or DiscordAcePerms
# Setup
**config.json**: There are two config files one for the FivePD Plugin and one for the service_status resource, they are identical to each other. Make sure after making changes to one you make the same changes to the other config file.\
The Service_Status resource depends on either DiscordAcePerms or Badger_Discord_API to display available services the player can select and go on duty for. If both DiscordAcePerms and Badger_Discord_API are set to true, the resource will default to use DiscordAcePerms as it is quicker to fetch player data.

**DiscordAcePerms**\
If you use DiscordAcePerms change the value of "use-ace-perms" to **true**.

**Badger_Discord_API**\
If you use Badger_Discord_API change the value of "use-discord-roles" to **true**.

**Adding a service**: There are three parts to adding a service but only two are relevant to the user depending on if youre using DiscordAcePerms or Badger_Discord_API.
1) The **Name** of the role you want to add. This name will be shown on the menu that players access to see which Services are Active/Inactive.
2) The **Ace-Perm** is the name of the Ace Permission you want to use to allow players to go on duty for that particular service.
3) The **Discord-Role** is the name of the Discord Role you want players to have in order to access that particular service.
