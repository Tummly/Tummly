type Closer = () => void

const peerClosers = new Set<Closer>()
let assistantCloser: Closer = () => {}

/** Bind the Assistant closer so page Drawers can dismiss it first. */
export function bindExclusiveAssistantCloser(close: Closer): () => void {
  assistantCloser = close
  return () => {
    if (assistantCloser === close) {
      assistantCloser = () => {}
    }
  }
}

/** Register a peer right Drawer closer. The Assistant adapter calls all peers. */
export function registerExclusivePeerCloser(close: Closer): () => void {
  peerClosers.add(close)
  return () => {
    peerClosers.delete(close)
  }
}

export function closeExclusivePeerRightDrawers(): void {
  for (const close of [...peerClosers]) {
    close()
  }
}

export function closeExclusiveAssistantDrawer(): void {
  assistantCloser()
}
