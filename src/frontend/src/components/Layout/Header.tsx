import { useAuth } from '../../context/AuthContext';
import styles from './Header.module.css';

export function Header() {
  const { user, logout } = useAuth();

  return (
    <header className={styles.header}>
      <h1 className={styles.title}>Life Sprint</h1>
      <div className={styles.userInfo}>
        {user?.avatarUrl && (
          <img
            src={user.avatarUrl}
            alt={user.gitHubUsername}
            className={styles.avatar}
          />
        )}
        <span className={styles.username}>{user?.gitHubUsername}</span>
        <button onClick={logout} className={styles.logoutButton}>
          Logout
        </button>
      </div>
    </header>
  );
}
