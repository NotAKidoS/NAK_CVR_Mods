using System.Collections;
using UnityEngine;

namespace NAK.DesktopVRSwitch;

class VRModeSwitchDebugger : MonoBehaviour
{
    private Coroutine _switchCoroutine;
    private WaitForSeconds _sleep = new WaitForSeconds(2.5f);

    private void OnEnable()
    {
        if (_switchCoroutine == null)
            _switchCoroutine = StartCoroutine(SwitchLoop());
    }

    private void OnDisable()
    {
        if (_switchCoroutine != null)
        {
            StopCoroutine(_switchCoroutine);
            _switchCoroutine = null;
        }
    }

    private IEnumerator SwitchLoop()
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
