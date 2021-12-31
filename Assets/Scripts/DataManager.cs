using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// https://forum.unity.com/threads/vector3-is-not-marked-serializable.435303

struct DataManager
{
    public static void Save()
    {
        // var settings = new JsonSerializerSettings
        // {
        //     ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        // };
        // Debug.Log(JsonConvert.SerializeObject(new Data(), settings));

        // JsonSerializer serializer = new JsonSerializer();
        // serializer.NullValueHandling = NullValueHandling.Ignore;
        //
        // using (StreamWriter sw = new StreamWriter(@"c:\temp\json.txt"))
        // using (JsonWriter writer = new JsonTextWriter(sw))
        // {
        //     serializer.Serialize(writer, new Coord());
        // }
        
        // return;
        
        // TODO: Check if there's anything to save
        
        // if (!Grid3D.IsValid())
        if (!TrackEditor.canTransformBeApplied)
        {
            Debug.Log("not valid, not saving");
            // TODO: Show message to user
            return;
        }

        Data dataToSave = new Data();
        
        if (!TrackEditor.canTransformBeApplied)
        {
            Debug.Log("track is empty, not saving");
            // TODO: Show message to user
            return;
        }
        
        BinaryFormatter binaryFormatter = new BinaryFormatter();
        // FileStream file = File.Create(Application.persistentDataPath + "/savedata.bin");
        FileStream file = File.Create("c:/temp/savedata.txt");
        binaryFormatter.Serialize(file, dataToSave);
        // binaryFormatter.Serialize(file, new Vector3());
        file.Close();
        Debug.Log(JsonConvert.SerializeObject(new Data()));
        // TODO: Show message to user
    }

    public static void Load()
    {
        // Get part from prefabs by tag
    }
}

[Serializable]
class Data
{
    // public List<GridCubeSaveData> gridCubesData;
    public List<PartSaveData> partsSaveData;

    /*  To save:  */
    // List of GridCubes, which are occupied (parts.Count = 1)
    //      • coordinates
    //      _parts will be filled from ↓
    // List of parts
    //      • occupiedGridCubes
    //      • _rotation
    //      • tag
    // TODO: Camera information?

    public Data()
    {
        // gridCubesData = Grid3D.GetPartsSaveData();
        partsSaveData = Part.GetPartsSaveData();
    }
}