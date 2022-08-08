using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    /// <summary>
    /// ステータス
    /// </summary>
    public enum PhotonState
    {
        INIT,
        CONNECTED,
        IN_LOBBY,
        READY,
        IN_GAME,
        DISCONNECTED,
        WAITING,
    }

    #region Singleton
    static PhotonManager _instance = null;

    public static bool IsEmpty
    {
        get { return _instance == null; }
    }

    public static PhotonManager Instance
    {
        get
        {
            if (_instance == null)
            {
                System.Type type = typeof(PhotonManager);
                _instance = GameObject.FindObjectOfType(type) as PhotonManager;
            }

            return _instance;
        }
    }
    #endregion

    public delegate void RoomJoinedCallback();
    public delegate void GameStartCallback();
    public delegate void GameEventCallback(int id, int evt);
    public delegate void RoomUpdateCallback(List<RoomInfo> roomList);

    [SerializeField, Tooltip("一緒に着けるPhotonView")] PhotonView _photonView;
    [SerializeField, Tooltip("1つのルームに何人までか")] int _maxPlayerInRoom = 2;
    [SerializeField, Tooltip("自動的に接続")] bool _isAutoConnect = true;
    [SerializeField, Tooltip("自動でルームに入る")] bool _isAutoJoin = true;
    [SerializeField, Tooltip("Photonで共有されるユーザ情報")] UserParam _mySelf = new UserParam();

    [SerializeField, Tooltip("ランクマになる")] bool _isRankMatching = true;
    [SerializeField, Tooltip("マッチング用合言葉")] string _onlineKeyword = "";
    
    private List<RoomInfo> _roomList = null;    //ルーム情報一覧
    PhotonState State;                          //Photonの接続State

    /// <summary>
    /// ゲーム中かどうか
    /// </summary>
    static public bool IsGameNow { get { return _instance && _instance.State == PhotonState.IN_GAME; } }

    //NOTE: ネットワークで共有するユーザーデータたち
    //ロビーでマッチング時に使用する情報
    List<UserParam> _player = new List<UserParam>();
    public UserParam Me => _mySelf;
    public UserParam GetPlayer(int id)
    {
        return _player[id];
    }
    
    // 状態管理
    List<int> _playerStatus = new List<int>();

    //コールバック群
    RoomJoinedCallback _roomJoinCallback;
    GameStartCallback _gameStartCallback;
    GameEventCallback _eventCallback;

#if UNITY_EDITOR
    public RoomUpdateCallback RoomUpdate { get; set; }
#endif

    private void Start()
    {
        //コールバックは都合のいいところの関数を設定するとよい
        _roomJoinCallback = RoomJoin;
        _gameStartCallback = GameStart;
        _eventCallback = GameEvent;
        //

        if (_isAutoConnect)
        {
            Connect();
        }
    }
    
    // ユーザ定義関数

    /// <summary>
    /// ルームに入ったときに呼ばれる処理
    /// </summary>
    void RoomJoin()
    {
        if (PhotonNetwork.IsMasterClient)
        {
        }

        //プレイヤー作成
        //PhotonNetwork.Instantiate("Player", new Vector3(UnityEngine.Random.Range(-40, 40), 1, UnityEngine.Random.Range(-40, 40)), Quaternion.identity);
    }

    void GameStart()
    {
        Debug.Log("GameStart");
    }

    /// <summary>
    /// イベント処理
    /// </summary>
    /// <param name="id"></param>
    /// <param name="evt"></param>
    void GameEvent(int id, int evt)
    {
        Debug.Log($"GameEvent: From{id} - Event:{evt}");
    }


    // Photonまわりの公開関数


    /// <summary>
    /// 接続開始(オフライン時は使用しない)
    /// </summary>
    public void Connect()
    {
        Debug.Log("Connect");

        //FPS調整
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 60;

        PhotonNetwork.NickName = _mySelf.Name;
        PhotonNetwork.ConnectUsingSettings();

        State = PhotonState.WAITING;
    }

    /// <summary>
    /// 接続終了
    /// </summary>
    public void Disconnect()
    {
        PhotonNetwork.Disconnect();
    }

    /// <summary>
    /// ロビーに入る
    /// NOTE: 外部から呼ばれることはない
    /// </summary>
    void JoinLobby()
    {
        PhotonNetwork.JoinLobby();
    }

    /// <summary>
    /// 部屋を作る
    /// </summary>
    public void CreateRoom()
    {
        Debug.Log("CreateRoom");
        RoomOptions roomOptions = new RoomOptions();

        //カスタムプロパティ
        List<string> properties = new List<string>();
        properties = properties.Concat(_mySelf.GetPropertiesString()).ToList();
        var roomProp = _mySelf.CreateHashTable();

        //UserParam以外で部屋の情報を共有したければここに追加する
        //

        //ここは変えない
        roomProp["GameState"] = 0;
        roomProp["Keyword"] = _onlineKeyword;
        properties = properties.Concat(new string[] { "GameState", "Keyword" }).ToList();
        
        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = (Byte)_maxPlayerInRoom;
        roomOptions.CustomRoomProperties = roomProp;
        roomOptions.CustomRoomPropertiesForLobby = properties.ToArray();
        PhotonNetwork.CreateRoom(Guid.NewGuid().ToString(), roomOptions, TypedLobby.Default);
    }

    /// <summary>
    /// 部屋から出る
    /// </summary>
    public void LeaveRoom()
    {
        Debug.Log("LeaveRoom");
        PhotonNetwork.LeaveRoom();
    }

    /// <summary>
    /// 指定した部屋に入る
    /// </summary>
    /// <returns></returns>
    public bool Matching(string roomName)
    {
        Debug.Log($"Select Matching: {roomName}");

        return PhotonNetwork.JoinRoom(roomName);
    }

    /// <summary>
    /// 適当な部屋に入る
    /// </summary>
    /// <returns></returns>
    public bool RandomMatching()
    {
        Debug.Log("RandomMatching");

        //入れる部屋を探す
        var list = MatchingFilter(_isRankMatching);

        //入れる部屋があったら適当に入る
        if (list.Count > 0)
        {
            Debug.Log("部屋があったので適当にはいる");
            return PhotonNetwork.JoinRoom(list[UnityEngine.Random.Range(0, list.Count)].Name);
        }

        return false;
    }

    /// <summary>
    /// マッチング時にルーム情報をフィルタする
    /// NOTE: ここいじってマッチング設定変えれる
    /// </summary>
    /// <returns></returns>
    List<RoomInfo> MatchingFilter(bool isRankMatching)
    {
        //同じランク帯、またはフレンドルームを見つける
        return _roomList.Where(info =>
        {
            //入れない部屋は除外
            if (info.PlayerCount >= _maxPlayerInRoom) return false;
            if (int.Parse(info.CustomProperties["GameState"].ToString()) == 1) return false;

            //合言葉を使っているかどうか
            if (_onlineKeyword != "")
            {
                if (info.CustomProperties["Keyword"] != null)
                {
                    if (_onlineKeyword == info.CustomProperties["Keyword"].ToString()) return true;
                }

                return false; //フレンドマッチはキーワードマッチ以外禁止
            }
            else
            {
                if (info.CustomProperties["Keyword"] != null)
                {
                    if (info.CustomProperties["Keyword"].ToString().Length > 0)
                    {
                        return false;
                    }
                }
            }

            //ランクマかどうか
            if (isRankMatching)
            {
                if (_mySelf.Rank == (int)info.CustomProperties["Rank"]) return true;
                return false;
            }
            else
            {
                return true;
            }
        }).ToList();
    }

    /// <summary>
    /// 状態更新
    /// </summary>
    private void Update()
    {
        switch (State)
        {
            //接続開始
            case PhotonState.CONNECTED:
                //接続したらすぐロビーに入る
                JoinLobby();
                State = PhotonState.WAITING;
                break;

            //ロビーで部屋選択
            case PhotonState.IN_LOBBY:
                {
                    //ルームリストもらうまで間があるので待機する
                    if (_roomList == null) break;

                    //オートマッチングなら勝手に部屋作ったり勝手に部屋はいったりする
                    if (_isAutoJoin)
                    {
                        //いまのロビーにいる人のルームリストをもらい、もらったルームからマッチング相手を探す
                        if (RandomMatching())
                        {
                            State = PhotonState.WAITING;
                        }
                        else
                        {
                            CreateRoom();
                            State = PhotonState.IN_GAME;
                        }
                    }
                }
                break;
                
            //ゲーム中
            case PhotonState.IN_GAME:
                break;
        }
    }


    /// <summary>
    /// ユーザ情報の更新
    /// </summary>
    void UpdateUserStatus()
    {
        Debug.Log("UpdateUserStatus");
        Photon.Realtime.Room room = PhotonNetwork.CurrentRoom;
        if (room == null)
        {
            return;
        }

        int index = 0;
        _player.Clear();
        foreach (var pl in room.Players.Values)
        {
            if (pl.CustomProperties["GUID"] == null) continue;

            _player.Add(new UserParam());
            _player[index++].UpdateHashTable(pl.CustomProperties);
        }
    }

    /// <summary>
    /// そろったかなチェック
    /// </summary>
    void CheckRoomStatus()
    {
        bool isGameStart = false;

        //そろっていたら始める
        if (PhotonNetwork.CurrentRoom.PlayerCount >= _maxPlayerInRoom) isGameStart = true;


        if (!isGameStart) return;

        //ゲーム開始したので乱入を禁止する
        PhotonNetwork.CurrentRoom.CustomProperties["GameState"] = 1;
        //開始時点のTickを記録。
        PhotonNetwork.CurrentRoom.CustomProperties["GameStartTime"] = PhotonNetwork.Time;
        PhotonNetwork.CurrentRoom.SetCustomProperties(PhotonNetwork.CurrentRoom.CustomProperties);

        //ゲーム開始のコールをする
        SendGameStart();
    }

    // Photon以外の対応

    /// <summary>
    /// 合言葉のセット
    /// </summary>
    /// <param name="kwd">合言葉</param>
    public void SetKeyword(string kwd)
    {
        _onlineKeyword = kwd;
    }


    //以下Photonからのコールバック

    /// <summary>
    /// Photonに接続したとき
    /// </summary>
    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster");
        State = PhotonState.CONNECTED;
    }

    /// <summary>
    /// ロビー入ったとき
    /// </summary>
    public override void OnJoinedLobby()
    {
        Debug.Log("OnJoinedLobby:" + PhotonNetwork.CurrentLobby.Name);
        
        //カスタムプロパティ
        var userProp = _mySelf.CreateHashTable();
        userProp["GUID"] = Guid.NewGuid().ToString();
        PhotonNetwork.SetPlayerCustomProperties(userProp);

        State = PhotonState.IN_LOBBY;
    }

    /// <summary>
    /// ルームに入ったとき
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom");
        base.OnJoinedRoom();

        _roomJoinCallback?.Invoke();

        //相手が来るまで待つよう同期する
        State = PhotonState.READY;
        UpdateUserStatus();
        CheckRoomStatus();
    }

    /// <summary>
    /// ルームから抜けたとき
    /// </summary>
    public override void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom");
        base.OnLeftRoom();

        State = PhotonState.IN_LOBBY;
    }

    /// <summary>
    /// PUN2でのルーム取得
    /// </summary>
    /// <param name="roomList">ルーム情報</param>
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("OnRoomListUpdate");

        base.OnRoomListUpdate(roomList);

        _roomList = roomList;

#if UNITY_EDITOR
        RoomUpdate?.Invoke(_roomList);
#endif
    }

    /// <summary>
    /// 相手の切断
    /// </summary>
    /// <param name="otherPlayer"></param>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        //相手が切断したので勝ちにする
    }
    

    //RPC

    /// <summary>
    /// イベント
    /// </summary>
    [PunRPC]
    void EventCall(int id, int status)
    {
        _eventCallback?.Invoke(id, status);
    }
    public void SendEvent(int evt)
    {
        _photonView.RPC("EventCall", RpcTarget.All, PhotonNetwork.LocalPlayer.UserId, evt);
    }

    [PunRPC]
    void GameStartCall()
    {
        State = PhotonState.IN_GAME;
        _gameStartCallback?.Invoke();
    }
    void SendGameStart()
    {
        _photonView.RPC("GameStartCall", RpcTarget.All);
    }
}