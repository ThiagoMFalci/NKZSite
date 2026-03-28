import {Routes, Route, Router} from "react-router-dom";
import Index from './Pages/Index'
import NotFound from './Pages/NotFound'
import Navbar from './Components/Navbar'

function App() {
    return (

        <>
            <Navbar />

            <Routes>
                <Route path="/"  element={<Index/>}/>
                <Route path="*" element={<NotFound/>}/>
            </Routes>

        </>
    )

}

export default App;