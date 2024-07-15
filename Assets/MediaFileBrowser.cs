using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.IO;
using SimpleFileBrowser;

public class MediaFileBrowser : MonoBehaviour
{
	public Mediapipe.Unity.Sample.AppSettings appSettings;
	public Dropdown sourceTypeDropdown;

	private static int previousDropdownValue = 0;

	void Start()
	{
		FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".jpg", ".png"), new FileBrowser.Filter("videos", ".mp4", ".mov"));
		FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
		FileBrowser.AddQuickLink("Users", "C:\\Users", null);
	}

	void Update()
	{

	}

	IEnumerator ShowLoadDialogCoroutine()
	{
		yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");

		if (FileBrowser.Success)
		{
			if (sourceTypeDropdown.value == 1)
			{
				byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(FileBrowser.Result[0]);
				appSettings._availableStaticImageSources[0].LoadImage(bytes);
			}
			if (sourceTypeDropdown.value == 2)
			{
				var media = new GameObject("Video Media Manager");
				var videoMediaManager = media.AddComponent<VideoMediaManager>();
				videoMediaManager.videoPath = "file://" + FileBrowser.Result[0];
			}
		}
	}


	public void DropdownValueChanged()
	{
		if (sourceTypeDropdown.value > 0 && sourceTypeDropdown.value != previousDropdownValue)
		{
			StartCoroutine(ShowLoadDialogCoroutine());
		}
		previousDropdownValue = sourceTypeDropdown.value;
	}
}