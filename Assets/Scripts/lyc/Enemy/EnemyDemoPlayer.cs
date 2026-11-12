using UnityEngine;
using UnityEngine.InputSystem;

namespace Asylum.Enemies
{
    [RequireComponent(typeof(CharacterController))]
    public class EnemyDemoPlayer : MonoBehaviour
    {
        public float speed=5;
        private Vector3 initialPosition;
        private void Start() => initialPosition=transform.position;
        private void Update()
        {
            var target=GetComponent<EnemyTarget>();
            var keys=Keyboard.current; if(keys==null) return;
            if(keys.rKey.wasPressedThisFrame)
            {
                var controller=GetComponent<CharacterController>(); controller.enabled=false;
                transform.position=initialPosition; controller.enabled=true;
                if(target!=null) target.alive=true;
            }
            if(target!=null && !target.alive) return;
            Vector3 movement=new Vector3((keys.dKey.isPressed||keys.rightArrowKey.isPressed?1:0)-(keys.aKey.isPressed||keys.leftArrowKey.isPressed?1:0),0,
                (keys.wKey.isPressed||keys.upArrowKey.isPressed?1:0)-(keys.sKey.isPressed||keys.downArrowKey.isPressed?1:0));
            GetComponent<CharacterController>().Move((movement.normalized*speed+Vector3.down*2)*Time.deltaTime);
        }
    }
}
