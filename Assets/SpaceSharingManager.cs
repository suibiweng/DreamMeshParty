#if FUSION2
using System;
using Fusion;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using TMPro;
using Object = UnityEngine.Object;

public class SpaceSharingManager : NetworkBehaviour
{
    [Networked] private NetworkString<_512> NetworkedRoomUuid { get; set; }
    
    [Networked] private NetworkString<_512> NetworkedRemoteFloorPose { get; set; }

    private Guid _sharedAnchorGroupID;

    public TextMeshProUGUI TMP; 

    public override void Spawned()
    {
        base.Spawned();
        PrepareColocation();
    }

    private void PrepareColocation()
    {
        if (Object.HasStateAuthority)
        {
            AdvertiseColocationSession();
        }
        else
        {
            DiscoverNearbySession();
        }
    }

    private async void AdvertiseColocationSession()
    {
        var result = await OVRColocationSession.StartAdvertisementAsync(null);

        if (!result.Success)
        {
            Debug.LogError($"SpaceSharingManager Failed to start advertisement: {result.Status}");
            TMP.text += "\n" + $"SpaceSharingManager Failed to start advertisement: {result.Status}";
            return;
        }

        _sharedAnchorGroupID = result.Value;
        print("SpaceSharingManager Advertisement started");
        TMP.text += "\n" + "SpaceSharingManager Advertisement started";

        ShareMrukRooms();
    }

    private async void ShareMrukRooms()
    {
        var room = MRUK.Instance.GetCurrentRoom();
        NetworkedRoomUuid = room.Anchor.Uuid.ToString();
        print("SpaceSharingManager ShareMrukRooms");
        TMP.text += "\n" + "SpaceSharingManager ShareMrukRooms";

        var result = await room.ShareRoomAsync(_sharedAnchorGroupID);

        if (!result.Success)
        {
            Debug.LogError($"SpaceSharingManager Failed to share mruk room: {result.Status}");
            TMP.text += "\n" + $"SpaceSharingManager Failed to share mruk room: {result.Status}";

            return;
        }
        print("SpaceSharingManager ShareMrukRooms success");
        TMP.text += "\n" + "SpaceSharingManager ShareMrukRooms success";


        var pose = room.FloorAnchor.transform;
        NetworkedRemoteFloorPose = 
            $"{pose.position.x},{pose.position.y},{pose.position.z},{pose.rotation.x}," + 
            $"{pose.rotation.y},{pose.rotation.z},{pose.rotation.w}";
        print("SpaceSharingManager ShareMrukRooms pose");
        TMP.text += "\n" + "SpaceSharingManager ShareMrukRooms pose";

    }

    private async void DiscoverNearbySession()
    {
        OVRColocationSession.ColocationSessionDiscovered += OnColocationSessionDiscovered;
        var result = await OVRColocationSession.StartDiscoveryAsync();

        if (!result.Success)
        {
            Debug.LogError($"SpaceSharingManager Failed to start discovery: {result.Status}");
            TMP.text += "\n" + $"SpaceSharingManager Failed to start discovery: {result.Status}";

        }
        else
        {
            print("SpaceSharingManager spaceSharing session started successfully");
            TMP.text += "\n" + "SpaceSharingManager spaceSharing session started successfully";

        }
    }

    private void OnColocationSessionDiscovered(OVRColocationSession.Data session)
    {
        OVRColocationSession.ColocationSessionDiscovered -= OnColocationSessionDiscovered;
        _sharedAnchorGroupID = session.AdvertisementUuid;
        print("SpaceSharingManager colocation session discovered");
        TMP.text += "\n" + "SpaceSharingManager colocation session discovered";

        LoadSharedRoom(_sharedAnchorGroupID);
    }

    private static Pose ParsePose(string poseString)
    {
        var parts = poseString.Split(',');
        if (parts.Length == 7)
        {
            return new Pose(
                new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2])),
                new Quaternion(
                    float.Parse(parts[3]), float.Parse(parts[4]), float.Parse(parts[5]), float.Parse(parts[6]))
            );
        }
        Debug.LogError($"SpaceSharingManager Failed to parse pose: {poseString}");
        
        return default;
    }

    private async void LoadSharedRoom(Guid groupUuid)
    {
        print("SpaceSharingManager Loading SharedRoom");
        TMP.text += "\n" + "SpaceSharingManager Loading SharedRoom";

        var roomUuid = Guid.Parse(NetworkedRoomUuid.ToString());
        var remotePoseStr = NetworkedRemoteFloorPose.ToString();
        var remoteFloorWorldPose = ParsePose(remotePoseStr);
        TMP.text += "\n" + "SpaceSharingManager Setup the loading variables";

        
        var result = 
            await MRUK.Instance.LoadSceneFromSharedRooms(null,  groupUuid, (roomUuid, remoteFloorWorldPose));
        TMP.text += "\n" + "SpaceSharingManager Just finished the await";

        if (result == MRUK.LoadDeviceResult.Success)
        {
            print("SpaceSharingManager Successfully loaded shared room");
            TMP.text += "\n" + "SpaceSharingManager Successfully loaded shared room";

        }
        else
        {
            Debug.LogError($"SpaceSharingManager Failed to load shared room: {result}");
            TMP.text += "\n" + $"SpaceSharingManager Failed to load shared room: {result}";

        }
    }
    
}
#endif