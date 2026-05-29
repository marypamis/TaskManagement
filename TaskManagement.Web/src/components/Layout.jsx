/**
 * Shared page wrapper with navbar for authenticated routes.
 */
import Navbar from './Navbar'

export default function Layout({ children }) {
  return (
    <div className="layout">
      <Navbar />
      <main className="container">{children}</main>
    </div>
  )
}
