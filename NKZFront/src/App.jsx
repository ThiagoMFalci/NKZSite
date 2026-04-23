import { Routes, Route} from "react-router-dom";
import Index from './Pages/Index'
import NotFound from './Pages/NotFound'
import Login from './Pages/Login'
import Navbar from './Components/Navbar'
import Footer from './Components/Footer'

function App() {
    return (

        <>

                <Navbar />

                <Routes>
                    <Route path="/"  element={<Index/>}/>
                    <Route path="/login" element={<Login/>}/>
                    <Route path="*" element={<NotFound/>}/>
                </Routes>

                <Footer />


        </>
    )

}

export default App;