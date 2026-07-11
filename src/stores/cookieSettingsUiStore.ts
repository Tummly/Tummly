import { create } from "zustand";

interface CookieSettingsUiState {
  isOpen: boolean;
  open: () => void;
  close: () => void;
  setOpen: (open: boolean) => void;
}

export const useCookieSettingsUiStore = create<CookieSettingsUiState>(
  (set) => ({
    isOpen: false,
    open: () => set({ isOpen: true }),
    close: () => set({ isOpen: false }),
    setOpen: (open) => set({ isOpen: open }),
  }),
);
