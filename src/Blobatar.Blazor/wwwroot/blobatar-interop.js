// Blobatar.Blazor: the bridge between the <Blobatar> component and upstream's gaze driver.
// Drivers are kept here by id so the component never has to hold a JavaScript object reference per blobatar.
import { gaze } from "./gaze.js";

const drivers = new Map();
let nextId = 1;
let observer = null;

// A safety net for a lost circuit: if the <svg> leaves the document and .NET never got to call stop(),
// tear the driver down so its window listeners and frame loop do not leak.
function watch() {
  if (observer || typeof MutationObserver === "undefined" || !document.body) return;
  observer = new MutationObserver(() => {
    for (const [id, entry] of drivers) {
      if (!entry.svg.isConnected) stop(id);
    }
    if (drivers.size === 0 && observer) {
      observer.disconnect();
      observer = null;
    }
  });
  observer.observe(document.body, { childList: true, subtree: true });
}

/** Starts a driver on an animated blobatar's <svg>. Returns 0 when the element is not one. */
export function attach(svg, settle, snap) {
  if (typeof SVGSVGElement === "undefined" || !(svg instanceof SVGSVGElement)) return 0;
  const options = {};
  if (settle != null) options.settle = settle;
  if (snap != null) options.snap = snap;
  const id = nextId++;
  drivers.set(id, { svg, driver: gaze(svg, options) });
  watch();
  return id;
}

/** Aims a driver: "point" (x, y in client coordinates), "element", "pointer", "rest", or anything else for nothing. */
export function lookAt(id, kind, x, y, element) {
  const entry = drivers.get(id);
  if (!entry) return;
  switch (kind) {
    case "point":
      entry.driver.lookAt({ x, y });
      break;
    case "element":
      if (element) entry.driver.lookAt(element);
      break;
    case "pointer":
      entry.driver.lookAt("pointer");
      break;
    case "rest":
      entry.driver.lookAt("rest");
      break;
    default:
      entry.driver.lookAt(null);
  }
}

/** Writes or removes the inline excursion on the <svg>; the eyes inherit it. */
export function setTravel(svg, travel) {
  if (!svg || !svg.style) return;
  if (travel == null) svg.style.removeProperty("--mo-track-travel");
  else svg.style.setProperty("--mo-track-travel", `${travel}px`);
}

export function remeasure(id) {
  drivers.get(id)?.driver.remeasure();
}

export function stop(id) {
  const entry = drivers.get(id);
  if (!entry) return;
  drivers.delete(id);
  entry.driver.stop();
}
