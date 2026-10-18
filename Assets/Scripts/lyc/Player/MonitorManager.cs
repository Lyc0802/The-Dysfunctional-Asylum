using UnityEngine;
using Fusion;

public class MonitorManager : NetworkBehaviour
{
    
    public Monitor[] monitors;
    [Networked] public int currentMonitor{get;set;}
    // [Networked] public NetworkObjectRef PlayerRef { get; set; }
    [Networked] public PlayerInfo currentUser{ get; set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void Spawned()
    {
        if (!HasStateAuthority) return;
        
        currentMonitor = -1;
        
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetTarget(PlayerInfo player)
    {
        currentUser = player;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetCurrentMonitor(int currentMonitor)
    {
        this.currentMonitor = currentMonitor;
    }

    // Update is called once per frame
    public override void Render()
    {
        if(currentUser == null || currentMonitor == -1) return;
        monitors[currentMonitor].SetRotation(currentUser.GetHeadRotation());
    }
}
