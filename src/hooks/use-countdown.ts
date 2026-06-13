import { useEffect, useState } from "react"

export function useCountdown(initialSeconds: number, active = true) {
  const [secondsRemaining, setSecondsRemaining] = useState(initialSeconds)

  useEffect(() => {
    if (!active) return

    setSecondsRemaining(initialSeconds)
  }, [active, initialSeconds])

  useEffect(() => {
    if (!active || secondsRemaining <= 0) return

    const timer = window.setInterval(() => {
      setSecondsRemaining((current) => Math.max(current - 1, 0))
    }, 1000)

    return () => window.clearInterval(timer)
  }, [active, secondsRemaining])

  const restart = (seconds = initialSeconds) => {
    setSecondsRemaining(seconds)
  }

  return {
    secondsRemaining,
    isComplete: secondsRemaining <= 0,
    restart,
  }
}
