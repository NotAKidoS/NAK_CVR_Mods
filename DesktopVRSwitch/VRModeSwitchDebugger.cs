using System.Collections;
using UnityEngine;

namespace NAK.DesktopVRSwitch;

internal class VRModeSwitchDebugger : MonoBehaviour
{
    private Coroutine _switchCoroutine;
    private WaitForSeconds _sleep;

    private void OnEnable()
    {
        if (_switchCoroutine == null)
        {
            _switchCoroutine = StartCoroutine(SwitchLoop());
            _sleep = new WaitForSeconds(2f);
        }
    }

    private void OnDisable()
    {
        if (_switchCoroutine != null)
        {
            StopCoroutine(_switchCoroutine);
            _switchCoroutine = null;
            _sleep = null;
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
