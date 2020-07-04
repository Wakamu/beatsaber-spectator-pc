using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IPA.Config.Data;
using UnityEngine;

namespace Quest_Spectator
{
	public enum BeatmapMode
	{
		Mode = 0,
		ModeOneSaber = 1,
		ModeNoArrows = 2,
		Mode360Degree = 3,
		Mode90Degree = 4
	}

	public class Packet
    {
		public int type;
		public Byte[] data;
    }

	public class SongInfo
	{
		public CustomPreviewBeatmapLevel beatmap;
		public GameplayModifiers modifiers;
		public BeatmapDifficulty difficulty;
		public bool leftHanded;
		public BeatmapMode mode;
	}

	public class ReplayLine
	{
		public Vector3 rightPosition;
		public Vector3 rightRotation;
		public Vector3 leftPosition;
		public Vector3 leftRotation;
		public Vector3 headPosition;
		public int score;
		public int combo;
		public float time;
		public float energy;
		public float rank;
	}

	public class ReplaySong
    {
		public SongInfo songInfo;
		public List<ReplayLine> replayLines;
    }

	/// <summary>
	/// Monobehaviours (scripts) are added to GameObjects.
	/// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
	/// </summary>
	public class Quest_SpectatorController : MonoBehaviour
	{
		public static Quest_SpectatorController instance { get; private set; }

		public TcpClient client;

		public ReplaySong replaySong;

		public int indexNum;
		public float songTime;
		public bool spectating;
		public float scoreMultiplier;
		public bool readyToSpectate;
		public bool once;
		Thread myThread;

		public static void ThreadLoop()
		{
			TcpClient client = Quest_SpectatorController.instance.client;
			while (Thread.CurrentThread.IsAlive)
			{
				if (client != null && client.Connected)
				{
					Byte[] data;
					data = new Byte[104];
					NetworkStream stream = client.GetStream();
					Int32 bytes = stream.Read(data, 0, data.Length);
					if (bytes == 0)
					{
						return;
					}
					Packet packet = DeserializePacket(data);
					if (packet.type == 1)
					{
						Logger.log?.Warn("MAKING SONGINFO");
						SongInfo songInfo = DeserializeSongInfo(packet.data);
						Logger.log?.Warn("DONE");
						if (songInfo.beatmap != null)
                        {
							Logger.log?.Warn("FOUND MAP");
							instance.replaySong = new ReplaySong();
							Logger.log?.Warn("received new song: " + Convert.ToString(songInfo.beatmap.songName));
							instance.replaySong.songInfo = songInfo;
							instance.replaySong.replayLines = new List<ReplayLine>();
							instance.spectating = true;
							LevelHelper.PlayLevel(songInfo);
						}
					}
					else if (instance.spectating)
					{
						ReplayLine replayLine = DeserializeReplayLine(packet.data);
						instance.replaySong.replayLines.Add(replayLine);
					}
				}
			}
		}

		#region Monobehaviour Messages
		/// <summary>
		/// Only ever called once, mainly used to initialize variables.
		/// </summary>
		private void Awake()
		{
			// For this particular MonoBehaviour, we only want one instance to exist at any time, so store a reference to it in a static property
			//   and destroy any that are created while one already exists.
			if (instance != null)
			{
				Logger.log?.Warn($"Instance of {this.GetType().Name} already exists, destroying.");
				GameObject.DestroyImmediate(this);
				return;
			}
			GameObject.DontDestroyOnLoad(this); // Don't destroy this object on scene changes
			instance = this;
			Logger.log?.Debug($"{name}: Awake()");
			indexNum = 0;
			spectating = false;
			scoreMultiplier = 1.0f;
			readyToSpectate = false;
			once = false;
		}
		/// <summary>
		/// Only ever called once on the first frame the script is Enabled. Start is called after any other script's Awake() and before Update().
		/// </summary>
		private void Start()
		{
			Logger.log?.Warn("QUESTSPECTATOR NOT READY FOR USE YET");
			replaySong = new ReplaySong();
			
		}

		public bool hasFakeMiss()
		{
			int amountCheckingEachSide = 2;

			int biggestCombo = 0;
			for (int i = -amountCheckingEachSide; i < (amountCheckingEachSide * 2) + 1; i++)
			{
				if (replaySong.replayLines[indexNum].combo <= 1)
				{
					return false;
				}
			}
			return true;
		}

		public void StartClient()
        {
			if (!once)
            {
				once = true;
				client = new TcpClient("192.168.1.1", 65123); // Quest IP and port
				Logger.log?.Warn("STARTED CLIENT");
				myThread = new Thread(new ThreadStart(ThreadLoop));
				myThread.Start();
			}
        }

		public static Packet DeserializePacket(Byte[] data)
        {
			Packet packet = new Packet();
			int type = BitConverter.ToInt32(data, 0);
			packet.type = type;
			packet.data = data.Skip(24).Take(80).ToArray();
			return packet;
        }

		public static SongInfo DeserializeSongInfo(Byte[] data)
		{
			SongInfo songInfo = new SongInfo();

			string hash = BitConverter.ToString(data, 40, 20).Replace("-", String.Empty);
			hash = hash.ToUpper();
			Logger.log?.Warn(hash);
			CustomPreviewBeatmapLevel beatmap;
			using (SHA1 sha1Hash = SHA1.Create())
			{

				beatmap = SongCore.Loader.CustomLevels.FirstOrDefault(t => BitConverter.ToString(sha1Hash.ComputeHash(Encoding.UTF8.GetBytes(t.Value.levelID.ToUpper()))).Replace("-", String.Empty) == hash).Value;
			}
			GameplayModifiers modifiers = new GameplayModifiers();
			modifiers.batteryEnergy = BitConverter.ToBoolean(data, 0);
			modifiers.disappearingArrows = BitConverter.ToBoolean(data, 1);
			modifiers.noObstacles = BitConverter.ToBoolean(data, 2);
			modifiers.noBombs = BitConverter.ToBoolean(data, 3);
			modifiers.noArrows = BitConverter.ToBoolean(data, 4);
			bool slowerSong = BitConverter.ToBoolean(data, 5);
			modifiers.songSpeed = GameplayModifiers.SongSpeed.Normal;
			if (slowerSong)
            {
				modifiers.songSpeed = GameplayModifiers.SongSpeed.Slower;
            }
			modifiers.noFail = BitConverter.ToBoolean(data, 6);
			modifiers.instaFail = BitConverter.ToBoolean(data, 7);
			modifiers.ghostNotes = BitConverter.ToBoolean(data, 8);
			bool fasterSong = BitConverter.ToBoolean(data, 9);
			if (fasterSong)
			{
				modifiers.songSpeed = GameplayModifiers.SongSpeed.Faster;
			}
			songInfo.leftHanded = BitConverter.ToBoolean(data, 10);
			songInfo.difficulty = (BeatmapDifficulty) BitConverter.ToInt32(data, 12);
			songInfo.mode = (BeatmapMode)BitConverter.ToInt32(data, 16);
			songInfo.beatmap = beatmap;
			songInfo.modifiers = modifiers;
			return songInfo;
		}

		public static Vector3 DeserializeVector(Byte[] data, int startIndex)
        {
			float x = BitConverter.ToSingle(data, startIndex);
			float y = BitConverter.ToSingle(data, startIndex + 4);
			float z = BitConverter.ToSingle(data, startIndex + 8);
			return new Vector3(x, y, z);
		}

		public static ReplayLine DeserializeReplayLine(Byte[] data)
		{
			ReplayLine replayLine = new ReplayLine();
			replayLine.rightPosition = DeserializeVector(data, 0);
			replayLine.rightRotation = DeserializeVector(data, 12);
			replayLine.leftPosition = DeserializeVector(data, 24);
			replayLine.leftRotation = DeserializeVector(data, 36);
			replayLine.headPosition = DeserializeVector(data, 48);
			replayLine.score = BitConverter.ToInt32(data, 60);
			replayLine.combo = BitConverter.ToInt32(data, 64);
			replayLine.time = BitConverter.ToSingle(data, 68);
			replayLine.energy = BitConverter.ToSingle(data, 72);
			replayLine.rank = BitConverter.ToSingle(data, 76);
			return replayLine;
		}

		/// <summary>
		/// Called every frame if the script is enabled.
		/// </summary>
		private void Update()
		{
			
		}

		/// <summary>
		/// Called every frame after every other enabled script's Update().
		/// </summary>
		private void LateUpdate()
		{

		}

		/// <summary>
		/// Called when the script becomes enabled and active
		/// </summary>
		private void OnEnable()
		{

		}

		/// <summary>
		/// Called when the script becomes disabled or when it is being destroyed.
		/// </summary>
		private void OnDisable()
		{

		}

		/// <summary>
		/// Called when the script is being destroyed.
		/// </summary>
		private void OnDestroy()
		{
			Logger.log?.Debug($"{name}: OnDestroy()");
			instance = null;
		}
		#endregion
	}
}
