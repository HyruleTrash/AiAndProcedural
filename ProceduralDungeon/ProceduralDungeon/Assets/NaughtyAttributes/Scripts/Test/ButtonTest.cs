using System.Collections;
using UnityEngine;

namespace NaughtyAttributes.Test
{
    public class ButtonTest : MonoBehaviour
    {
        public int myInt;

        [Button(enabledMode: EButtonEnableMode.Always)]
        private void IncrementMyInt()
        {
            this.myInt++;
        }

        [Button("Decrement My Int", EButtonEnableMode.Editor)]
        private void DecrementMyInt()
        {
            this.myInt--;
        }

        [Button(enabledMode: EButtonEnableMode.Playmode)]
        private void LogMyInt(string prefix = "MyInt = ")
        {
            Debug.Log(prefix + this.myInt);
        }

        [Button("StartCoroutine")]
        private IEnumerator IncrementMyIntCoroutine()
        {
            int seconds = 5;
            for (int i = 0; i < seconds; i++)
            {
                this.myInt++;
                yield return new WaitForSeconds(1.0f);
            }
        }
    }
}
