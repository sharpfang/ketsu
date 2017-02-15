﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;

/* Map parser executes once when you open the scene,
 * if the scene is new, i.e. has not been initialized before.
 * 
 * If the scene has been initialized before, the easiest way to run 
 * Map parser again is to uncheck Initialized and duplicate the 
 * GameObject holding the Map parser.
 *
 * The Map file used must be in .json format,
 * and is loaded based on user's selection.
 */

//[ExecuteInEditMode]
public class ParseMapOnLoad : MonoBehaviour {

	//map structure to hold information
	public class Map {
		public int height;
    	public int width;
    	public int amountOfTiles;
    	//dictionary of <layer name, layer data>
    	public Dictionary<string, long[,]> tiles;
	}

    [SerializeField]
    private bool initialized = false;
 
    void Awake() { Initialize(); }
 
    void Initialize()
    {
    	//load again
        if (initialized){
        	Destroy(GameObject.Find("Ground"));
        	Destroy(GameObject.Find("Obstacles"));
        	Destroy(GameObject.Find("Objects"));
        }
 
		//init new map
		Map map = new Map();
		map.tiles = new Dictionary<string, long[,]>();
		string tileString = "";
		string layername = "";
		

		//read json file
		string line = null;

		//which map to read, ask user
		//string path = "Assets/Maps/01Tutorial01LearnToMoveUpwards.json";
		TextAsset file = Resources.Load("01Tutorial01LearnToMoveUpwards") as TextAsset;
		string path = file.text;

		//StreamReader reader = new StreamReader(path);
		StringReader reader = new StringReader(path);
		while ((line = reader.ReadLine()) != null) {	        	
			int start = line.IndexOf(":", 0)+1;
        	int end = line.IndexOf(",", start);

	        if(line.Contains("data")){
	        	tileString = line.Substring(start+1, line.LastIndexOf(',')-2-start);

				while ((line = reader.ReadLine()) != null) {
			        //parse the variables
					start = line.IndexOf(":", 0)+1;
		        	end = line.IndexOf(",", start);

		        	//layer name
					if(line.Contains("name"))
			        	layername = line.Substring(start+1, line.LastIndexOf(',')-2-start);
			   		//height & width for the map
			        if(line.Contains("\"height") && map.height == 0)
			        	int.TryParse(line.Substring(start, end - start), out map.height);
			        if(line.Contains("\"width")){
			        	if(map.width == 0){
			        		int.TryParse(line.Substring(start, end - start), out map.width);
			        		break;
			        	}
			        	else
			        		break;
			        }
			        
				}

				//parse the string of tiles to an actual array
			    long[,] layerdata = parseTiles(tileString, map.width, map.height);
				map.tiles.Add(layername, layerdata);
	        }
	        if(line.Contains("tilecount"))
	        	int.TryParse(line.Substring(start, end - start), out map.amountOfTiles);
	        
	    }
	    //instantiate GameObjects via prefabs
	    createObjects(map.tiles, map.width, map.height);

        initialized = true;
	}

	long[,] parseTiles(string tileString, int x, int y){
		tileString = Regex.Replace(tileString, @"\s+", "");
		string[] substrings = tileString.Split(',');

		long[,] tiles = new long[y, x];
		for(int i=0; i<y; i++)
			for(int j=0; j<x; j++)
				long.TryParse(substrings[i*x+j], out tiles[i,j]);
		
		return tiles;
	}

	Dictionary<int, GameObject> loadPrefabsFromFile(){
		Dictionary<int, GameObject> prefabs = new Dictionary<int, GameObject>();

		TextAsset file = Resources.Load("tiles") as TextAsset;
		string path = file.text;

		//StreamReader reader = new StreamReader("Assets/Maps/tiles.txt");
		StringReader reader = new StringReader(path);
		string line = null;

		while ((line = reader.ReadLine()) != null) {
			int n = 0;
			int.TryParse(line.Substring(0, line.IndexOf(' ')), out n);
			string name = line.Substring(line.LastIndexOf(' ')+1);

			GameObject prefab = (GameObject)Resources.Load("Prefabs/"+name);
			prefabs.Add(n, prefab);
		}	

		return prefabs;
	}

	void createObjects(Dictionary<string, long[,]> tiles, int x, int y){

		Dictionary<int, GameObject> prefabs = loadPrefabsFromFile();
		//Rotations 2684354560 == 90deg (0xA), 3221225472 == 180 deg (0xC), 1610612736 == 270 (0x6)
		long deg90 = 2684354560;
		long deg180 = 3221225472;
		long deg270 = 1610612736;
		//Create correct objects
        foreach(KeyValuePair<string, long[,]> entry in tiles){

        	//Instantiate ground things
			if(entry.Key=="Ground"){
				GameObject GroundParent = new GameObject();
				GroundParent.name = "Ground";
				for(int i=0; i<y; i++){
					for(int j=0; j<x; j++){
						for(int k=1; k<=30; k++)
							if(prefabs.ContainsKey(k) && entry.Value[y-i-1,x-j-1]==k){
								GameObject item = Instantiate(prefabs[k], new Vector3(-j+x-1, 0, i), Quaternion.identity);
								item.transform.parent = GroundParent.transform;
							}	
					}
				}
			}
			 
			//Instantiate obstacles
			if(entry.Key=="Obstacles"){
				GameObject ObstacleParent = new GameObject();
				ObstacleParent.name = "Obstacles";
				for(int i=0; i<y; i++){
					for(int j=0; j<x; j++){
						for(int k=31; k<=80; k++)
							if(prefabs.ContainsKey(k) && entry.Value[y-i-1,x-j-1]==k){
								GameObject item = Instantiate(prefabs[k], new Vector3(-j+x-1, 0, i), Quaternion.Euler(0, Random.Range(-360.0f, 0.0f), 0));
								item.transform.parent = ObstacleParent.transform;
							}
					}
				}
			}

			//Instantiate objects = spawn points etc.
			if(entry.Key=="Objects"){
				GameObject ObjectParent = new GameObject();
				ObjectParent.name = "Objects";
				GameObject item;
				for(int i=0; i<y; i++){
					for(int j=0; j<x; j++){
						if(prefabs.ContainsKey(141)){
							if(entry.Value[y-i-1,x-j-1] == 141){
								item = Instantiate(prefabs[141], new Vector3(-j+x-1, 0, i), Quaternion.identity);
								item.transform.parent = ObjectParent.transform;
							}
							else if(entry.Value[y-i-1,x-j-1] == 141+deg90){
								item = Instantiate(prefabs[141], new Vector3(-j+x-1, 0, i), Quaternion.Euler(0, 90, 0));
								item.transform.parent = ObjectParent.transform;
							}
							else if(entry.Value[y-i-1,x-j-1] == 141+deg180){
								item = Instantiate(prefabs[141], new Vector3(-j+x-1, 0, i), Quaternion.Euler(0, 180, 0));
								item.transform.parent = ObjectParent.transform;
							}
							else if(entry.Value[y-i-1,x-j-1] == 141+deg270){
								item = Instantiate(prefabs[141], new Vector3(-j+x-1, 0, i), Quaternion.Euler(0, 270, 0));
								item.transform.parent = ObjectParent.transform;
							}
						}
						if(prefabs.ContainsKey(191)){
							if(entry.Value[y-i-1,x-j-1] == 191){
								item = Instantiate(prefabs[191], new Vector3(-j+x-1, 0, i), Quaternion.identity);
								item.transform.parent = ObjectParent.transform;
							}
							else if(entry.Value[y-i-1,x-j-1] == 191+deg90){
								item = Instantiate(prefabs[191], new Vector3(-j+x-1, 0, i), Quaternion.Euler(0, 90, 0));
								item.transform.parent = ObjectParent.transform;
							}
							else if(entry.Value[y-i-1,x-j-1] == 191+deg180){
								item = Instantiate(prefabs[191], new Vector3(-j+x-1, 0, i), Quaternion.Euler(0, 180, 0));
								item.transform.parent = ObjectParent.transform;
							}
							else if(entry.Value[y-i-1,x-j-1] == 191+deg270){
								item = Instantiate(prefabs[191], new Vector3(-j+x-1, 0, i), Quaternion.Euler(0, 270, 0));
								item.transform.parent = ObjectParent.transform;
							}
						}
						if(prefabs.ContainsKey(192)){
							if(entry.Value[y-i-1,x-j-1] == 192){
								item = Instantiate(prefabs[192], new Vector3(-j+x-1, 0, i), Quaternion.identity);
								item.transform.parent = ObjectParent.transform;
							}
							else if(entry.Value[y-i-1,x-j-1] == 192+deg90){
								item = Instantiate(prefabs[192], new Vector3(-j+x-1, 0, i), Quaternion.Euler(0, 90, 0));
								item.transform.parent = ObjectParent.transform;
							}
							else if(entry.Value[y-i-1,x-j-1] == 192+deg180){
								item = Instantiate(prefabs[192], new Vector3(-j+x-1, 0, i), Quaternion.Euler(0, 180, 0));
								item.transform.parent = ObjectParent.transform;
							}
							else if(entry.Value[y-i-1,x-j-1] == 192+deg270){
								item = Instantiate(prefabs[192], new Vector3(-j+x-1, 0, i), Quaternion.Euler(0, 270, 0));
								item.transform.parent = ObjectParent.transform;
							}
						}
						if(prefabs.ContainsKey(181) && entry.Value[y-i-1,x-j-1] == 181){
							item = Instantiate(prefabs[181], new Vector3(-j+x-1, 0, i), Quaternion.identity);
							item.transform.parent = ObjectParent.transform;
						}
						if(prefabs.ContainsKey(182) && entry.Value[y-i-1,x-j-1] == 182){
							item = Instantiate(prefabs[182], new Vector3(-j+x-1, 0, i), Quaternion.identity);
							item.transform.parent = ObjectParent.transform;
						}
					}
				}
				if(prefabs.ContainsKey(193)){
					item = Instantiate(prefabs[193], new Vector3(0,0,0), Quaternion.identity);
					item.transform.parent = ObjectParent.transform;
				}
			}
		}
	}

}
