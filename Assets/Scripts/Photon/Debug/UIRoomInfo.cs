using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;

class UIRoomInfo : MonoBehaviour
{
    [SerializeField] Text _roomName;
    [SerializeField] Text _userName;
    [SerializeField] Text _playerCount;
    [SerializeField] Button _joinButton;

    RoomInfo _roomInfoCache;

    public void UpdateRoomInfo(RoomInfo info)
    {
        _roomInfoCache = info;
        
        string roomName = info.Name;
        string keyword = info.CustomProperties["Keyword"]?.ToString();

        if (keyword != null && keyword.Length > 0)
        {
            _roomName.text = "[鍵付き]" + roomName;
        }
        else
        {
            _roomName.text = roomName;
        }

        _userName.text = info.CustomProperties["Name"]?.ToString();
        _playerCount.text = String.Format("{0}/{1}", info.PlayerCount, info.MaxPlayers);

        _joinButton.onClick.RemoveAllListeners();
        _joinButton.onClick.AddListener(() =>
        {
            PhotonManager.Instance.Matching(_roomInfoCache.Name);
        });
    }
}
