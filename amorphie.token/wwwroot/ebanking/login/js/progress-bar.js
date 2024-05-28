// on page load...
moveProgressBar();

// SIGNATURE PROGRESS
function moveProgressBar() {
  let currentSec = 0;
  let targetSecs = 180;

  let interval = setInterval(() => {
    currentSec += 1;

    if (currentSec <= targetSecs) {
      let percent = currentSec / targetSecs * 100;
      document.getElementsByClassName('progress-bar')[0].style.width = `${percent}%`;
      document.getElementsByClassName('progress-text')[0].innerHTML = targetSecs - currentSec;
    } else {
      clearInterval(interval);
      document.getElementsByClassName('progress-wrap')[0].style.display = 'none';
    }
  }, 1000);
}