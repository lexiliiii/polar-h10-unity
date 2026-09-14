# polar-h10-unity


## Run

1. Open **polar-h10-unity with Unity 2022.3.56f1**.
2. In PowerShell, start the supplied local bridge and prebuilt acquisition page:

   ```powershell
   node "path_to_this_folder\polar-h10-unity\Tools\PolarH10Bridge\server.mjs"
   ```

3. Open **http://localhost:8080**. Keep this browser open throughout acquisition. The in-app browser was used only to check page loading.
4. Click **Connect Polar H10**. Enable **EXG**. Choose one of these Bodypart values:

   | Browser Bodypart | Unity region |
   |---|---|
   | LShoulder | Left Shoulder |
   | RShoulder | Right Shoulder |
   | MiddleSpine | Middle Spine |
   | LowerSpine | Lower Spine |

5. In the row's filter settings, use **highpass**, **200 ms RMS window**. Select **200ms RMS** to compare with the browser graph. RMS is exported whenever EXG is running, regardless of which graph view is selected. Recording and folder selection are not required.
6. In Unity, choose **Tools → Polar H10 → Open Dashboard Scene**, then Play.
7. Readings arrive automatically. Set **Min** and **Max** and click **Apply range** to adjust color sensitivity. The Inspector fields `rmsMinimumUv` and `rmsMaximumUv` set scene defaults. Runtime edits last for the current Play/player session; save Inspector defaults outside Play mode for persistence.

One H10 measures one location at a time. Use **four H10 devices for simultaneous readings at four locations**, each with one browser row and a different Bodypart. With one H10, change Bodypart when moving it; its previous location clears. Use one acquisition browser tab and one Unity receiver/player at a time. Duplicating a physical sensor as multiple browser rows with different filter settings is not supported by this adapter because upstream does not export a row identifier.

Gray means waiting or stale, not measured zero. Each reading expires after two seconds without current data. Zero RMS remains a valid live measurement. Numerical readings preserve the received double-precision RMS; only color/bar intensity is normalized.

The default **0–120 µV** range comes from the upstream graph constants. It is not a measured physiological range or hardware maximum. No real recording was supplied to estimate a personal range. Adjust the minimum to a suitable resting baseline and maximum to the range you want to visualize. Values outside this range remain numerically unchanged while color intensity clamps to 0–1.