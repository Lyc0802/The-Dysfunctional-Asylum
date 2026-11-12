using UnityEngine;
using Fusion;

public class MonitorManager : NetworkBehaviour
{
    public static MonitorManager Instance;
    public Monitor[] monitors;
    [Networked, OnChangedRender(nameof(OnCurrentMonitorChanged))]
    public int currentMonitor { get; set; }
    // [Networked] public NetworkObjectRef PlayerRef { get; set; }
    [Networked] public PlayerInfo currentUser{ get; set; }

    void Awake()
    {
        if(Instance != null)
        {
            return;
        }
        Instance = this;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void Spawned()
    {
        if (!HasStateAuthority) return;
        if(Instance != null && Instance != this)
        {
            Runner.Despawn(this.Object);
            return;
        }
        // currentMonitor = -1;
        
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SendRequest(PlayerInfo player)
    {

        bool isApproved = currentUser == null;
        Debug.Log("Monitor: is Request " + isApproved);

        if (isApproved) currentUser = player;
        player.isUsingMonitor = true;
        RPC_SendRespond(player.Object.InputAuthority, isApproved, true, currentUser);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SendRelease(PlayerInfo player)
    {
        bool isApproved = currentUser == player;
        Debug.Log("Monitor: is Request " + isApproved);


        // player.SetIsUsingMonitor(false);
        player.isUsingMonitor = false;
    
        RPC_SendRespond(player.Object.InputAuthority, isApproved, false, currentUser);
        if (isApproved) currentUser = null;
    }

    // [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    // public void RPC_Release()
    // {
    //     currentUser = null;
    // }

    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SendRespond([RpcTarget] PlayerRef target, bool isApproved, bool isGetIn, PlayerInfo player)
    {
        if (isApproved)
        {
            if (isGetIn)
            {
                if(player != null && player.Object.HasInputAuthority)
                {
                    Vector3 v = monitors[currentMonitor].transform.position;
                    v.y  -= 1.2f; 
                    player.vrRig.transform.position = v;
                    player.vrRig.GetComponent<VRCameraSeatFollower>().isUsingMonitor = true;
                    player.SetIsUsingMonitor(true);
                }

            }
            else
            {
                if(player != null && player.Object.HasInputAuthority)
                {
                    player.vrRig.transform.localPosition = new Vector3(0f, -0.6f, 0f);
                    player.vrRig.GetComponent<VRCameraSeatFollower>().isUsingMonitor = false;
                    player.SetIsUsingMonitor(false);
                    Debug.Log("Monitor: back");
                    // RPC_Release();
                }
            }
        }
        else
        {
            // 拒絕使用監視器
        }
    }
    private void OnCurrentMonitorChanged()
    {
        if (!currentUser.HasInputAuthority) return;
        Vector3 v = monitors[currentMonitor].transform.position;
        v.y  -= 1.2f; 
        currentUser.vrRig.transform.position = v;
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetCurrentMonitor(int currentMonitor)
    {
        // if (currentUser != player) return;
        this.currentMonitor = currentMonitor;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddCurrentMonitor(int amount, PlayerInfo player)
    {
        if (currentUser != player) return;
        currentMonitor += amount + monitors.Length;
        currentMonitor %= monitors.Length;
    }

    public bool isUsed()
    {
        return MonitorManager.Instance.currentUser != null;

    }

    // Update is called once per frame
    public override void Render()
    {
        if(currentUser == null || currentMonitor == -1) return;
        monitors[currentMonitor].SetRotation(currentUser.GetHeadRotation());
    }
}
