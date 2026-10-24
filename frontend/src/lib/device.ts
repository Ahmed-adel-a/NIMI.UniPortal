const STORAGE_KEY = 'nimi.device';

/**
 * A random id for this browser, kept in local storage: attendance binds one device per student (decision DEVICE), and
 * this is how the browser recognises itself. Clearing site data makes it a new device.
 */
export function deviceFingerprint(): string {
  try {
    let id = localStorage.getItem(STORAGE_KEY);
    if (!id) {
      id = crypto.randomUUID();
      localStorage.setItem(STORAGE_KEY, id);
    }
    return id;
  } catch {
    return 'no-storage';
  }
}
