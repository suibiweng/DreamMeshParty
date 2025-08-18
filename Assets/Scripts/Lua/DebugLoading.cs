using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLoading : MonoBehaviour
{
    public LuaMonoBehavior luaMonoBehavior;
    // Start is called before the first frame update
    // void  Start()
    // {
    //     if (luaMonoBehavior != null)
    //     {
    //        // luaMonoBehavior.DebugRunJson(luaMonoBehavior.debugJson);
          
    //     }
    // }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(2f);
        if (luaMonoBehavior != null)
        {
            luaMonoBehavior.DebugRunJson(luaMonoBehavior.debugJson);

            yield return new WaitForSeconds(3f);

            luaMonoBehavior.Play();

        }
    }




    // Update is called once per frame
    void Update()
    {
        
    }
}
