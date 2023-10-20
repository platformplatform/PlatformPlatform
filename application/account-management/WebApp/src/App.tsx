import { Button } from "react-aria-components";

function App() {
  return (
    <div className="w-screen h-screen bg-slate-300 flex flex-col p-2">
      <Button
        className="bg-slate-500 p-2 rounded-md text-white text-sm border border-black hover:bg-slate-400 w-fit"
        onPress={() => alert("Create tenant")}
      >
        Create tenant!
      </Button>
    </div>
  );
}

export default App;
