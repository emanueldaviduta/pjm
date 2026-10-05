export enum TaskStatus {
  Created = 0,
  InProgress = 1,
  Completed = 2,
  OnHold = 3,
}

export enum TaskPriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3,
}

export interface TaskItem {
  id: number;
  projectId: number;
  assignedId: number;
  title: string;
  description: string;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

export type NewTaskItem = Omit<TaskItem, 'id' | 'createdAt' | 'updatedAt'>;

export const STATUS_LABELS: Record<TaskStatus, string> = {
  [TaskStatus.Created]: 'To do',
  [TaskStatus.InProgress]: 'In progress',
  [TaskStatus.OnHold]: 'On hold',
  [TaskStatus.Completed]: 'Done',
};

export const PRIORITY_LABELS: Record<TaskPriority, string> = {
  [TaskPriority.Low]: 'Low',
  [TaskPriority.Medium]: 'Medium',
  [TaskPriority.High]: 'High',
  [TaskPriority.Critical]: 'Critical',
};

/** Order of the columns on the project board. */
export const BOARD_STATUSES: TaskStatus[] = [
  TaskStatus.Created,
  TaskStatus.InProgress,
  TaskStatus.OnHold,
  TaskStatus.Completed,
];
