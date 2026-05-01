import { Navigate, Routes, Route, useLocation } from "react-router-dom";
import Index from './Pages/Index'
import DashboardPage from './Pages/Dashboard'
import TeamsPage from './Pages/Teams'
import PlayersPage from './Pages/Players'
import PlayerProfilePage from './Pages/PlayerProfile'
import RankingPage from './Pages/Ranking'
import LeaguesPage from './Pages/Leagues'
import LeaguePage from './Pages/LeaguePage'
import TournamentsPage from './Pages/Tournaments'
import NotificationsPage from './Pages/Notifications'
import WalletPage from './Pages/Wallet'
import Login from './Pages/Auth/Login'
import Register from './Pages/Auth/Register'
import ForgotPassword from './Pages/Auth/ForgotPassword'
import NotFound from './Pages/NotFound'
import Navbar from './Components/Navbar'
import Footer from './Components/Footer'
import { isAuthenticated } from "./utils/auth";

function ProtectedRoute({ children }) {
    const location = useLocation();
    if (!isAuthenticated()) {
        return <Navigate to="/login" replace state={{ from: location }} />;
    }

    return children;
}

function App() {
    return (

        <>

                <Navbar />

                <Routes>
                    <Route path="/"  element={<Index/>}/>
                    <Route path="/dashboard" element={<ProtectedRoute><DashboardPage/></ProtectedRoute>}/>
                    <Route path="/teams" element={<ProtectedRoute><TeamsPage/></ProtectedRoute>}/>
                    <Route path="/players" element={<ProtectedRoute><PlayersPage/></ProtectedRoute>}/>
                    <Route path="/players/:playerId" element={<ProtectedRoute><PlayerProfilePage/></ProtectedRoute>}/>
                    <Route path="/ranking" element={<ProtectedRoute><RankingPage/></ProtectedRoute>}/>
                    <Route path="/leagues" element={<ProtectedRoute><LeaguesPage/></ProtectedRoute>}/>
                    <Route path="/leagues/:leagueId" element={<ProtectedRoute><LeaguePage/></ProtectedRoute>}/>
                    <Route path="/tournaments" element={<ProtectedRoute><TournamentsPage/></ProtectedRoute>}/>
                    <Route path="/notifications" element={<ProtectedRoute><NotificationsPage/></ProtectedRoute>}/>
                    <Route path="/wallet" element={<ProtectedRoute><WalletPage/></ProtectedRoute>}/>
                    <Route path="/login" element={<Login/>}/>
                    <Route path="/register" element={<Register/>}/>
                    <Route path="/forgot-password" element={<ForgotPassword/>}/>
                    <Route path="*" element={<ProtectedRoute><NotFound/></ProtectedRoute>}/>
                </Routes>

                <Footer />


        </>
    )

}

export default App;
