import { Outlet } from 'react-router-dom';
import { Header } from './Header';
import { Sidebar } from './Sidebar';
import styles from './MainLayout.module.css';

export function MainLayout() {
  return (
    <div className={styles.layout}>
      <Header />
      <div className={styles.body}>
        <Sidebar />
        <main className={styles.main}>
          <Outlet />
        </main>
      </div>
    </div>
  );
}
