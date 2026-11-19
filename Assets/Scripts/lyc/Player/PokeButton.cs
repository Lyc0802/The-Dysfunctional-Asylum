using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Local poke button. Invokes On Click once when a press reaches Select.</summary>
[DisallowMultipleComponent]
public class PokeButton : MonoBehaviour
{
    [SerializeField] private PokeInteractable pokeInteractable;
    [Tooltip("Invoked once when the button is pressed. Drag scene objects here on a scene instance.")]
    [SerializeField] private UnityEvent onClick = new UnityEvent();

    public UnityEvent OnClick => onClick;

    private void Awake()
    {
        if (pokeInteractable == null)
            pokeInteractable = GetComponentInChildren<PokeInteractable>(true);
        if (pokeInteractable == null)
        {
            Debug.LogError("PokeButton requires a PokeInteractable.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (pokeInteractable != null) pokeInteractable.WhenStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        if (pokeInteractable != null) pokeInteractable.WhenStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(InteractableStateChangeArgs change)
    {
        // State transitions aggregate multiple fingers; holding Select does not repeat.
        if (isActiveAndEnabled && change.NewState == InteractableState.Select &&
            change.PreviousState != InteractableState.Select)
            onClick.Invoke();
    }
}
