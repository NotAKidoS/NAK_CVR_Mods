# LuaNetworkVariables

Adds a simple module for creating network variables & events *kinda* similar to Garry's Mod.

Example Usage:
```lua
-- Requires UnityEngine and NetworkModule
UnityEngine = require("UnityEngine")
NetworkModule = require("NetworkModule")

-- Unity Events --

function Start()

    if NetworkModule == nil then
        print("NetworkModule did not load.")
        return
    end

    -- Registers "AvatarHeight" as a network variable
    -- This creates Get and Set functions (GetAvatarHeight() and SetAvatarHeight())
    NetworkModule:RegisterNetworkVar("AvatarHeight")

    -- Registers a callback for when "AvatarHeight" is changed.
    NetworkModule:RegisterNotifyCallback("AvatarHeight", function(varName, oldValue, newValue)
        print(varName .. " changed from " .. tostring(oldValue) .. " to " .. tostring(newValue))
    end)

    -- Registers "ButtonClickedEvent" as a networked event. This provides context alongside the arguments passed.
    NetworkModule:RegisterEventCallback("ButtonClickedEvent", function(context, message)
        print("ButtonClickedEvent triggered by " .. tostring(context.SenderName) .. " with message: " .. tostring(message))
        print("Context details:")
        print("  SenderId: " .. tostring(context.SenderId))
        print("  SenderName: " .. tostring(context.SenderName))
        print("  LastInvokeTime: " .. tostring(context.LastInvokeTime))
        print("  TimeSinceLastInvoke: " .. tostring(context.TimeSinceLastInvoke))
        print("  IsLocal: " .. tostring(context.IsLocal))
    end)

    -- Secondry example
    NetworkModule:RegisterEventCallback("CoolEvent", OnCoolEventOccured)
end

function Update()
    if not NetworkModule:IsSyncOwner() then
        return
    end

    SetAvatarHeight(PlayerAPI.LocalPlayer:GetViewPointPosition().y)
end

-- Global Functions --

function SendClickEvent()
    NetworkModule:SendLuaEvent("ButtonClickedEvent", "The button was clicked!")
    print("Sent ButtonClickedEvent")
end

function SendCoolEvent()
    NetworkModule:SendLuaEvent("CoolEvent", 1, 2)
end

-- Listener Functions --

function OnCoolEventOccured(context, value, value2)
    print("CoolEvent triggered by " .. tostring(context.SenderName))
    print("Received values: " .. tostring(value) .. ", " .. tostring(value2))
    print("Context details:")
    print("  SenderId: " .. tostring(context.SenderId))
    print("  LastInvokeTime: " .. tostring(context.LastInvokeTime))
    print("  TimeSinceLastInvoke: " .. tostring(context.TimeSinceLastInvoke))
    print("  IsLocal: " .. tostring(context.IsLocal))
end
```

---

Here is the block of text where I tell you this mod is not affiliated with or endorsed by ABI. 
https://documentation.abinteractive.net/official/legal/tos/#7-modding-our-games

> This mod is an independent creation not affiliated with, supported by, or approved by Alpha Blend Interactive. 

> Use of this mod is done so at the user's own risk and the creator cannot be held responsible for any issues arising from its use.

> To the best of my knowledge, I have adhered to the Modding Guidelines established by Alpha Blend Interactive.
