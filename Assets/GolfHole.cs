using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using Fusion;

public class GolfHole : MonoBehaviour
{
   public ParticleSystem ExplodeParticles;

   private FindSpawnPositions FindSpawnPositions;
   
   private NetworkRunner _runner;


   private void Start()
   {
      FindSpawnPositions = FindObjectOfType<FindSpawnPositions>();
      _runner = FindObjectOfType<NetworkRunner>();

   }


   private void OnTriggerEnter(Collider other)
   {
      if (other.gameObject.CompareTag("GolfBall"))
      {
         ExplodeParticles.Play();
         FindSpawnPositions.StartSpawn();
         GameObject TempHole = FindObjectOfType<GolfHole>().gameObject;
         transform.position = TempHole.transform.position;
         transform.rotation = TempHole.transform.rotation;
         Destroy(TempHole);
         //try just moving the object. 
      }
      //need to move the hole to a new spot using the find spawn positions script
   }
   
   
   
   
}
