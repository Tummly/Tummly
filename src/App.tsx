import AppRoutes from "./pages/routes/AppRoutes";
import { Toaster } from "@/components/ui/sonner";

function App() {
  return (
    <>
      <AppRoutes />
      <Toaster richColors closeButton position="top-right" />
    </>
  );
}

export default App;
