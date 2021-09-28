var images = []
var songLength = 0;

const weaponModNames = ["Pistol", "Revolver", "Boomstick", "Burstfire", "Brawler"];

Connect();

//Attempt to reconnect until connection is acheived
//Used so you dont have to refresh overlay after starting game
function Connect()
{
  var socket = new WebSocket('ws://localhost:9000');

  socket.onopen = function () 
  {
    console.log("success"); 
  };

  socket.onmessage = function (msg) 
  { 
    console.log(msg.data); 
    let message = JSON.parse(msg.data);
    switch (message.Type) {
      case "Modifiers":
        HandleModifiersSet(message.Content);
        break;
      case "Song":
        HandleSongSet(message.Content);
        break;
      case "Difficulty":
        HandleDifficultySet(message.Content.Difficulty);
        break;
      case "ScoreUpdate":
        HandleScoreUpdate(message.Content);
        break;
      case "SongProgress":
        HandleSongProgress(message.Content.Time);
        break;
      case "ScoreFinal":
        HandleScoreUpdate(message.Content);
        HandleSongProgress(0);//Resets the timer
        break;
      case "Hits":
        HandleHits(message.Content); //Just a number
        break;
      case "HitsTaken":
        HandleHitsTaken(message.Content); //Just a number
        break;
      case "Config":
        HandleSetConfig(message.Content);
      break;
      case "GameStart":
        //HandleScoreUpdate({Score: 0, OnBeat: 1.0, Accuracy: 1.0});
      break;
      default:
        console.log("Not known packet, dumping content");
        console.log(msg.data); 
        break;
    }
  };

  socket.onclose = function (e) 
  { 
    console.log('Socket is closed. Reconnect will be attempted in 1 second.', e.reason);
    setTimeout(function() {
      Connect();
    }, 1000);
  };

  socket.onerror = function(err)
  {
    console.error('Socket encountered error: ', err.message, 'Closing socket');
    socket.close();
  }
}

function HandleModifiersSet(content)
{
  console.log("Setting modifiers");
  let container = document.getElementById("Modifiers");

  //Clear up
  images.forEach(element => {
    container.removeChild(element);
  });
  images = [];


  let names = content.Names;
  let imgs = content.IconsAsPNG;
  //Rearrange modifiers to weapons are first
  for (let i = 0; i < names.length; i++) {
    if (weaponModNames.find(w => w === names[i]))
    {
      if (i > 0)
      {
        let tmpName = names[0];
        names[0] = names[i];
        names[i] = tmpName;

        let tmpImg = imgs[0];
        imgs[0] = imgs[i];
        imgs[i] = tmpImg;
      }
      break;
    }
  }

  //Repeat for duals
  //We start at 1 since weapon is at 0 now
  for (let i = 1; i < names.length; i++) {
    if (names[i] === "Dual Wield")
    {
      if (i > 1)
      {
        let tmpName = names[1];
        names[1] = names[i];
        names[i] = tmpName;

        let tmpImg = imgs[1];
        imgs[1] = imgs[i];
        imgs[i] = tmpImg;
      }
      break;
    }
  }


  imgs.forEach(e => {
    var image = new Image();
    //Just getting the source from the span. It was messy in JS.
    image.src = "data:image/png;base64," + e;
    image.width = 50;
    image.height = 50;
    container.appendChild(image);
    images.push(image);
  });
}

function HandleSongSet(song)
{
  console.log("Song set");
  let img = document.getElementById("Poster");
  img.src = "images/" + song.Icon;

  let artists = document.getElementById("Song-Artist");
  let songName = document.getElementById("Song-Name");
  let bpm = document.getElementById("Song-BPM");
  artists.innerHTML = song.Artists;
  songName.innerHTML = song.Name;
  bpm.innerHTML = song.BPM;
  songLength = song.Length;
}

function HandleDifficultySet(difficulty)
{
  console.log("Difficulty set " + difficulty);
  let diff = document.getElementById("Difficulty");
  diff.innerHTML = difficulty;
}

function HandleScoreUpdate(info)
{
  let score = document.getElementById("Score");
  let beat = document.getElementById("OnBeat");
  let accuracy = document.getElementById("Accuracy");

  score.textContent = info.Score;
  
  if (info.OnBeat == 1)
    beat.textContent = (info.OnBeat*100).toFixed(0);
  else
    beat.textContent = (info.OnBeat*100).toFixed(2);

  if (info.Accuracy == 1)
    accuracy.textContent = (info.Accuracy*100).toFixed(0);
  else
    accuracy.textContent = (info.Accuracy*100).toFixed(2);
}

function HandleSongProgress(progress)
{
  let timerText = document.getElementById("Song-Progress");
  timerText.innerText = `${TimeToMinuteSeconds(progress)}`;

  const circumference = 90* Math.PI * 2;

  var bar = document.getElementById("Progress");
  bar.setAttribute("style", `stroke-dashoffset: ${(1-(progress/songLength))*circumference}px;`);
}


function HandleHits(hits)
{
  let hitsValue = document.getElementById("HitsValue");
  hitsValue.innerText = hits; 
}


function HandleHitsTaken(hitsTaken)
{
  let hitsTakenValue = document.getElementById("HitsTakenValue");
  hitsTakenValue.innerText = hitsTaken;
}

function HandleSetConfig(config)
{
  if (config.EnableHits == true)
  {
    let hitsElement = document.getElementById("HitsGroup");
    if (hitsElement.classList.contains("Hidden"))
      hitsElement.classList.remove("Hidden");
  }
  else 
  {
    let hitsElement = document.getElementById("HitsGroup");
    if (!hitsElement.classList.contains("Hidden"))
      hitsElement.classList.add("Hidden")
  }

  if (config.EnableHitsTaken == true)
  {
    let hitsTakenElement = document.getElementById("HitsTakenGroup");
    if (hitsTakenElement.classList.contains("Hidden"))
    hitsTakenElement.classList.remove("Hidden");
  }
  else
  {
    let hitsTakenElement = document.getElementById("HitsTakenGroup");
    if (!hitsTakenElement.classList.contains("Hidden"))
      hitsTakenElement.classList.add("Hidden")
  }

  //Determines whether we wanna handle the Return To Menu message from the game
  //TODO: Implement
  if (config.EnableToggleOverlayInMenu)
  {

  }
}

// Utility functions

function TimeToMinuteSeconds(time)
{
  time = time.toFixed(0);
  let minutes = Math.floor(time/60);
  let seconds = time%60;

  if(seconds < 10)
    seconds = `0${seconds}`;

  return `${minutes}:${seconds}`;
}

