using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

//#define EVENT_HANDLER false

public class XR_UI_Haptic_Feedback : MonoBehaviour
#if true
, IPointerEnterHandler, IPointerDownHandler
#endif
    {
#if true
    enum PointerEventType {
        HOVER,
        SELECT
    };

    public void OnPointerEnter(PointerEventData EventData) {
        TriggerHapticEvent(EventData, PointerEventType.HOVER);
    }

    public void OnPointerDown(PointerEventData EventData) {
        TriggerHapticEvent(EventData, PointerEventType.SELECT);
    }

    private void TriggerHapticEvent(PointerEventData EventData, PointerEventType EventType) {
        XRUIInputModule InputModule = EventSystem.current.currentInputModule as XRUIInputModule;
        if (InputModule == null) {
            return;
        }

        NearFarInteractor Interactor = InputModule.GetInteractor(EventData.pointerId) as NearFarInteractor;
        if (Interactor == null) {
            return;
        }

        float Duration = 0.1f;
        float Amplitude = 0.25f;
        switch (EventType) {
            case PointerEventType.HOVER: {
                Amplitude = 0.25f;
            } break;
            case PointerEventType.SELECT: {
                Amplitude = 0.5f;
            } break;
        }

        Interactor.SendHapticImpulse(Amplitude, Duration);
    }
#endif
}
