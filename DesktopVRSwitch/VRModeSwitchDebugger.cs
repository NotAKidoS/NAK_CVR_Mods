using System.Collections;
using UnityEngine;

namespace NAK.DesktopVRSwitch;

class VRModeSwitchDebugger : MonoBehaviour
{
    Coroutine _switchCoroutine;
    WaitForSeconds _sleep;

    void OnEnable()
    {
        if (_switchCoroutine == null)
        {
            _switchCoroutine = StartCoroutine(SwitchLoop());
            _sleep = new WaitForSeconds(2f);
        }
    }

    void OnDisable()
    {
        if (_switchCoroutine != null)
        {
            StopCoroutine(_switchCoroutine);
            _switchCoroutine = null;
            _sleep = null;
        }
    }

    IEnumerator SwitchLoop()
    {
        while (true)
        {
            if (!VRModeSwitchManager.Instance.SwitchInProgress)
            {
                VRModeSwitchManager.Instance.AttemptSwitch();
                yield return _sleep;
            }
            yield return null;
        }
    }
}
