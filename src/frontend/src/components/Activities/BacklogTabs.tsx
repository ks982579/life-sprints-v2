import { ContainerType } from '../../types';
import './BacklogTabs.css';

interface BacklogTabsProps {
  activeTab: ContainerType;
  onTabChange: (tab: ContainerType) => void;
}

const tabLabels: Record<number, string> = {
  [ContainerType.Annual]: 'Annual Backlog',
  [ContainerType.Monthly]: 'Monthly Backlog',
  [ContainerType.Weekly]: 'Weekly Sprint',
  [ContainerType.Daily]: 'Daily Checklist',
};

export function BacklogTabs({ activeTab, onTabChange }: BacklogTabsProps) {
  // For now, we'll show Annual, Monthly, and Weekly as requested
  const visibleTabs = [
    ContainerType.Annual,
    ContainerType.Monthly,
    ContainerType.Weekly,
  ];

  return (
    <div className="backlog-tabs">
      {visibleTabs.map((tab) => (
        <button
          key={tab}
          className={`tab-button ${activeTab === tab ? 'active' : ''}`}
          onClick={() => onTabChange(tab)}
        >
          {tabLabels[tab]}
        </button>
      ))}
    </div>
  );
}
