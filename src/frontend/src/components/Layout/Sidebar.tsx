import { NavLink } from 'react-router-dom';
import styles from './Sidebar.module.css';

export function Sidebar() {
  return (
    <nav className={styles.sidebar}>
      <div className={styles.section}>
        <span className={styles.sectionLabel}>Backlogs</span>
        <NavLink
          to="/annual"
          className={({ isActive }) => `${styles.navLink}${isActive ? ` ${styles.active}` : ''}`}
        >
          Annual
        </NavLink>
        <NavLink
          to="/monthly"
          className={({ isActive }) => `${styles.navLink}${isActive ? ` ${styles.active}` : ''}`}
        >
          Monthly
        </NavLink>
        <NavLink
          to="/weekly"
          className={({ isActive }) => `${styles.navLink}${isActive ? ` ${styles.active}` : ''}`}
        >
          Weekly Sprint
        </NavLink>
        <NavLink
          to="/daily"
          className={({ isActive }) => `${styles.navLink}${isActive ? ` ${styles.active}` : ''}`}
        >
          Daily Checklist
        </NavLink>
      </div>
    </nav>
  );
}
