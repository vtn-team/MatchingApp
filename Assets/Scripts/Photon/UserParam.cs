using System;

/// <summary>
/// 共有する情報
/// </summary>
[Serializable]
public struct UserParam
{
    public string Name; // ユーザ名
    public int Rank;    // レート

    //以下も併せて適宜書き換える事

    public string[] GetPropertiesString()
    {
        //下で定義するHashTableのキーと同じ値を入れること
        return new string[] {
            "Name",
            "Rank"
        };
    }

    public ExitGames.Client.Photon.Hashtable CreateHashTable()
    {
        ExitGames.Client.Photon.Hashtable roomProp = new ExitGames.Client.Photon.Hashtable();
        
        //ここに共有したい情報を入れる
        roomProp["Name"] = Name;
        roomProp["Rank"] = Rank;

        return roomProp;
    }

    public void UpdateHashTable(ExitGames.Client.Photon.Hashtable table)
    {
        ExitGames.Client.Photon.Hashtable roomProp = new ExitGames.Client.Photon.Hashtable();

        //ここに上書きする情報を入れる
        Name = table["Name"].ToString();
        Rank = Int32.Parse(table["Rank"].ToString());
    }
};