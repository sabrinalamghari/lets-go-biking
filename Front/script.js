const burger  = document.querySelector('.burger');
const header  = document.querySelector('header');
const scrim   = document.querySelector('.scrim');
const asideEl = document.querySelector('aside');
const chapters = document.querySelector('#chapters');
const mql = window.matchMedia('(orientation: portrait)');

function relocateChapters(){
  if (!chapters) return;
  if (mql.matches){
    if (chapters.parentElement !== header){
      header.appendChild(chapters);
    }
  } else {
    if (chapters.parentElement !== asideEl){
      asideEl.insertBefore(chapters, asideEl.firstChild);
    }
  }
}

function openMenu(){
  header.classList.add('open');
  burger.classList.add('open');            
  burger.setAttribute('aria-expanded', 'true');
  if (scrim){ scrim.hidden = false; scrim.offsetHeight; scrim.classList.add('show'); }
}
function closeMenu(){
  header.classList.remove('open');
  burger.classList.remove('open');     
  burger.setAttribute('aria-expanded', 'false');
  if (scrim){ scrim.classList.remove('show'); setTimeout(()=>scrim.hidden=true,1000); }
}
burger.addEventListener('click', () => {
  header.classList.contains('open') ? closeMenu() : openMenu();
});
if (scrim){ scrim.addEventListener('click', closeMenu); }

relocateChapters();                     
mql.addEventListener('change', relocateChapters); 
