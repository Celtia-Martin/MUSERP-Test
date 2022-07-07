using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Class of a object that can be in a pool 
[Serializable]
public class PoolObject
{
    public string type;
    public GameObject prefab;
    public int size;

}
