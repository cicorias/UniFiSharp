﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniFiSharp.Protocol;

namespace UniFiSharp
{
    /// <summary>
    /// Default implementation for UniFiApi, with HttpClient
    /// </summary>
    public class UniFiApi : UniFiApi<HttpClientConnectivityProvider>
    {
        public UniFiApi(Uri baseUri, string username, string password, string siteName = "default") : base(baseUri, username, password, siteName) { }
    }

    /// <summary>
    /// Basic API for UniFi Controller
    /// </summary>
    public partial class UniFiApi<T> where T : IConnectivityProvider
    {
        /// <summary>
        /// Site this API wrapper will use
        /// </summary>
        public string Site { get; set; }

        /// <summary>
        /// If the wrapper has successfully authenticated
        /// </summary>
        public bool IsAuthenticated { get; private set; }

        private string _username, _password;
        private IConnectivityProvider ConnectivityProvider { get; set; }

        /// <summary>
        /// Create an API wrapper
        /// </summary>
        /// <param name="baseUri">Controller URI</param>
        /// <param name="username">Controller username</param>
        /// <param name="password">Controller password</param>
        /// <param name="siteName">Site name</param>
        public UniFiApi(Uri baseUri, string username, string password, string siteName = "default")
        {
            ConnectivityProvider = (IConnectivityProvider)Activator.CreateInstance(typeof(T), baseUri, true);
            ConnectivityProvider.AuthenticationRequired += async (sender, e) => await Authenticate();

            Site = siteName;
            IsAuthenticated = false;
            _username = username;
            _password = password;
        }

        /// <summary>
        /// List all UniFi devices
        /// </summary>
        /// <param name="macAddress">UniFi Device MAC address (optional)</param>
        /// <returns>List of devices</returns>
        public async Task<List<Protocol.NetworkDevice>> ListDevices(string macAddress = null)
        {
            return await ConnectivityProvider.Get<Protocol.NetworkDevice>(AsSiteRequest($"/stat/device/{macAddress}"));
        }

        /// <summary>
        /// Run authentication against the controller
        /// </summary>
        /// <returns></returns>
        public async Task Authenticate()
        {
            await ConnectivityProvider.Post<object, BlankMessage>("/api/login", new
            {
                username = _username,
                password = _password,
                remember = false,
                strict = true
            });

            IsAuthenticated = true;
        }

        /// <summary>
        /// Toggle the status of the locator LED on the UniFi device
        /// </summary>
        /// <param name="macAddress">UniFi Device MAC</param>
        /// <param name="isFlashing"><c>TRUE</c> if LED is on, otherwise <c>FALSE</c></param>
        /// <returns></returns>
        public async Task LocateApToggle(string macAddress, bool isFlashing)
        {
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest("/cmd/devmgr"), new
            {
                cmd = isFlashing ? "set-locate" : "unset-locate",
                mac = macAddress
            });
        }

        /// <summary>
        /// Toggle the status of the site LED (LED on all devices)
        /// </summary>
        /// <param name="isOn"><c>TRUE</c> if LED is on, otherwise <c>FALSE</c></param>
        /// <returns></returns>
        public async Task SiteLedToggle(bool isOn)
        {
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest("/set/setting/mgmt"), new
            {
                led_enabled = isOn
            });
        }

        /// <summary>
        /// Set the device name
        /// </summary>
        /// <param name="deviceId">Device ID (from the _id parameter)</param>
        /// <param name="name">New device name</param>
        /// <returns></returns>
        public async Task SetName(string deviceId, string name)
        {
            await ConnectivityProvider.Put<object>(AsSiteRequest($"/rest/device/{deviceId}"), new
            {
                @name = name
            });
        }

        /// <summary>
        /// Force a client to reconnect
        /// </summary>
        /// <param name="macAddress">Client MAC Address</param>
        /// <returns></returns>
        public async Task ReconnectClient(string macAddress)
        {
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest("/cmd/stamgr"), new
            {
                cmd = "kick-sta",
                mac = macAddress
            });
        }

        /// <summary>
        /// List all clients
        /// </summary>
        /// <param name="macAddress">Client MAC Address (optional)</param>
        /// <returns>List of clients</returns>
        public async Task<List<Protocol.Client>> ListClients(string macAddress = null)
        {
            return await ConnectivityProvider.Get<Protocol.Client>(AsSiteRequest($"/stat/sta/{macAddress}"));
        }

        /// <summary>
        /// ??????????????????
        /// </summary>
        /// <param name="macAddress"></param>
        /// <returns></returns>
        public async Task<Protocol.Client> GetClientInfo(string macAddress)
        {
            return (await ConnectivityProvider.Get<Protocol.Client>(AsSiteRequest($"/stat/user/{macAddress}"))).FirstOrDefault();
        }

        /// <summary>
        /// ????????????????????
        /// </summary>
        /// <returns></returns>
        public async Task<Protocol.BlankMessage> GetHealth()
        {
            throw new NotImplementedException("Not yet implemented!");
            return (await ConnectivityProvider.Get<Protocol.BlankMessage>(AsSiteRequest("/stat/health"))).FirstOrDefault();
        }

        /// <summary>
        /// Get dashboard overview statistics
        /// </summary>
        /// <returns>Dashboard overview statistics</returns>
        public async Task<Protocol.Dashboard> GetDashboard()
        {
            return (await ConnectivityProvider.Get<Protocol.Dashboard>(AsSiteRequest($"/stat/dashboard"))).FirstOrDefault();
        }

        /// <summary>
        /// Get a list of rogue access points from the last N hours
        /// </summary>
        /// <param name="lastNHours">Number of hours to search</param>
        /// <returns>List of APs</returns>
        public async Task<List<Protocol.BlankMessage>> GetRogueAps(int lastNHours)
        {
            throw new NotImplementedException("Not yet implemented!");
            return await ConnectivityProvider.Get<Protocol.BlankMessage>(AsSiteRequest($"/stat/rogueap?within={lastNHours}"));
        }

        /// <summary>
        /// Get a list of wireless networks
        /// </summary>
        /// <returns>List of WLANs</returns>
        public async Task<List<Protocol.Wlan>> ListWlans()
        {
            return await ConnectivityProvider.Get<Protocol.Wlan>(AsSiteRequest("/list/wlanconf"));
        }

        /// <summary>
        /// Get a list of WLAN Groups
        /// </summary>
        /// <returns>List of WLAN groups</returns>
        public async Task<List<Protocol.WlanGroup>> ListWlanGroups()
        {
            return await ConnectivityProvider.Get<Protocol.WlanGroup>(AsSiteRequest("/list/wlangroup"));
        }

        /// <summary>
        /// Get a list of user groups
        /// </summary>
        /// <returns>List of user groups</returns>
        public async Task<List<Protocol.UserGroup>> ListUserGroups()
        {
            return await ConnectivityProvider.Get<Protocol.UserGroup>(AsSiteRequest("/list/usergroup"));
        }

        /// <summary>
        /// Create a WLAN group
        /// </summary>
        /// <param name="name">Name of WLAN group</param>
        /// <returns></returns>
        public async Task CreateWlanGroup(string name)
        {
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest("/rest/wlangroup"), new
            {
                roam_radio = "ng",
                roam_channel_na = 36,
                roam_channel_ng = 1,
                pmf_mode = "disabled",
                name = name
            });
        }

        /// <summary>
        /// Delete a WLAN group
        /// </summary>
        /// <param name="id">ID of group to delete</param>
        /// <returns></returns>
        public async Task DeleteWlanGroup(string id)
        {
            await ConnectivityProvider.Delete(AsSiteRequest($"/rest/wlangroup/{id}"));
        }

        /// <summary>
        /// Create a user group
        /// </summary>
        /// <param name="name">Name of user group</param>
        /// <returns></returns>
        public async Task CreateUserGroup(string name)
        {
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest("/rest/usergroup"), new
            {
                qos_rate_max_down = -1,
                qos_rate_max_up = -1,
                name = name
            });
        }

        /// <summary>
        /// Delete a user group
        /// </summary>
        /// <param name="id">ID of group to delete</param>
        /// <returns></returns>
        public async Task DeleteUserGroup(string id)
        {
            await ConnectivityProvider.Delete(AsSiteRequest($"/rest/usergroup/{id}"));
        }

        /// <summary>
        /// Create a wireless network SSID
        /// </summary>
        /// <param name="name">SSID of network</param>
        /// <param name="key">WPA2 key</param>
        /// <param name="userGroupId">User group ID</param>
        /// <param name="wlanGroupId">WLAN group ID</param>
        /// <returns></returns>
        public async Task CreateWlan(string name, string key, string userGroupId, string wlanGroupId)
        {
            if (key.Length < 8 || key.Length > 63)
                throw new Exception("Key must be between 8 and 63 characters.");
            
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest("/add/wlanconf"), new
            {
                name = name,
                x_passphrase = key,
                usergroup_id = userGroupId,
                wlangroup_id = wlanGroupId,
                enabled = true
            });
        }

        /// <summary>
        /// Delete a wireless network SSID
        /// </summary>
        /// <param name="wlanId">WLAN ID</param>
        /// <returns></returns>
        public async Task DeleteWlan(string wlanId)
        {
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest($"/del/wlanconf/{wlanId}"), new object[0] { });
        }

        /// <summary>
        /// Create a port forward
        /// </summary>
        /// <param name="name">Forwarding name</param>
        /// <param name="proto">tcp or udp</param>
        /// <param name="source">Source IP or "any"</param>
        /// <param name="dest">Destination IP</param>
        /// <param name="fromPort">External port</param>
        /// <param name="toPort">Internal port</param>
        /// <returns></returns>
        public async Task CreatePortForward(string name, string proto, string source, string dest, int fromPort, int toPort)
        {
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest("/rest/portforward"), new
            {
                name = name,
                proto = proto,
                dst_port = toPort,
                fwd_port = fromPort,
                fwd = dest,
                src = source
            });
        }

        /// <summary>
        /// Get a list of port forwards
        /// </summary>
        /// <returns>List of port forwards</returns>
        public async Task<List<Protocol.PortForward>> ListPortForwards()
        {
            return await ConnectivityProvider.Get<Protocol.PortForward>(AsSiteRequest("/list/portforward"));
        }

        /// <summary>
        /// Adopt a device
        /// </summary>
        /// <param name="macAddress">Device MAC</param>
        /// <returns></returns>
        public async Task Adopt(string macAddress)
        {
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest("/cmd/devmgr"), new
            {
                mac = macAddress,
                cmd = "adopt"
            });
        }

        /// <summary>
        /// Forget a device
        /// </summary>
        /// <param name="macAddress">Device MAC</param>
        /// <returns></returns>
        public async Task Forget(string macAddress)
        {
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest("/cmd/sitemgr"), new
            {
                mac = macAddress,
                cmd = "delete-device"
            });
        }

        /// <summary>
        /// Upgrade the firmware on a device to the latest known by the controller
        /// </summary>
        /// <param name="macAddress">Device MAC</param>
        /// <returns></returns>
        public async Task Upgrade(string macAddress)
        {
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest("/cmd/devmgr/upgrade"), new { mac = macAddress });
        }

        /// <summary>
        /// Run a RF Scan on an AP (takes AP offline for 5-10 minutes)
        /// </summary>
        /// <param name="macAddress">AP MAC</param>
        /// <returns></returns>
        public async Task RFScan(string macAddress)
        {
            await ConnectivityProvider.Post<object, BlankMessage>(AsSiteRequest("/cmd/devmgr"), new
            {
                mac = macAddress,
                cmd = "spectrum-scan"
            });
        }

        /// <summary>
        /// Get the status of a running RF scan
        /// </summary>
        /// <param name="macAddress">AP MAC</param>
        /// <returns>RF Scan Status</returns>
        public async Task<Protocol.RFSpectrumScan> RFScanStatus(string macAddress)
        {
            return (await ConnectivityProvider.Get<Protocol.RFSpectrumScan>(AsSiteRequest($"/stat/spectrum-scan/{macAddress}"))).FirstOrDefault();
        }

        /// <summary>
        /// Log out of API
        /// </summary>
        /// <returns></returns>
        public async Task Logout()
        {
            await ConnectivityProvider.Post<object, BlankMessage>("/api/logout", new { });
        }

        private string AsSiteRequest(string relativeUri)
        {
            return $"/api/s/{Site}{relativeUri}";
        }
    }
}