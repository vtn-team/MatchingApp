using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;

public class UIRoomList : MonoBehaviour
{
    [SerializeField] UIRoomInfo _prefab;
    [SerializeField] GameObject _listRoot;

#if UNITY_EDITOR
    private void Start()
    {
        PhotonManager.Instance.RoomUpdate = RoomUpdate;
    }
    
    void RoomUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("UpdateRoom");

        var list = _listRoot.GetComponentsInChildren<UIRoomList>().ToList();
        list.ForEach(ui => Destroy(ui.gameObject));

        roomList.ForEach(room =>
        {
            var script = Instantiate(_prefab, _listRoot.transform);
            script.UpdateRoomInfo(room);
        });
    }
#endif
}
