import { Injectable } from '@angular/core';
import {
  InstructorStudentDetail,
  InstructorStudentSummary,
  IntegrityFlag,
} from '../models/instructor.model';
import { ClassProgress } from '../models/progress.model';
import { ProgressService } from './progress.service';

@Injectable({ providedIn: 'root' })
export class InstructorService {
  constructor(private progressService: ProgressService) {}

  getClassProgress(): ClassProgress {
    return this.progressService.getClassProgress();
  }

  getStudents(): InstructorStudentSummary[] {
    return MOCK_STUDENTS;
  }

  getStudentById(id: string): InstructorStudentDetail | undefined {
    return MOCK_STUDENT_DETAILS.find(student => student.id === id);
  }

  getIntegrityFlags(): IntegrityFlag[] {
    return MOCK_INTEGRITY_FLAGS;
  }
}

const MOCK_STUDENTS: InstructorStudentSummary[] = [
  {
    id: 's1',
    name: 'Karim Ahmed',
    initials: 'KA',
    avgScore: 92,
    problemsSolved: 38,
    integrityStatus: 'clean',
  },
  {
    id: 's2',
    name: 'Layla Mostafa',
    initials: 'LM',
    avgScore: 88,
    problemsSolved: 34,
    integrityStatus: 'clean',
  },
  {
    id: 's3',
    name: 'Omar Sherif',
    initials: 'OS',
    avgScore: 85,
    problemsSolved: 31,
    integrityStatus: 'flagged',
  },
  {
    id: 's4',
    name: 'Sara Mahmoud',
    initials: 'SM',
    avgScore: 81,
    problemsSolved: 29,
    integrityStatus: 'review',
  },
];

const MOCK_INTEGRITY_FLAGS: IntegrityFlag[] = [
  {
    id: 'f1',
    studentId: 's3',
    studentName: 'Omar Sherif',
    severity: 'high',
    reason: 'Submission matches AI-generated pattern (87% confidence)',
    detectedAt: '2026-07-23T10:00:00Z',
  },
  {
    id: 'f2',
    studentId: 's4',
    studentName: 'Sara Mahmoud',
    severity: 'medium',
    reason: 'Code structure similar to another student submission',
    detectedAt: '2026-07-22T14:30:00Z',
  },
  {
    id: 'f3',
    studentId: 's1',
    studentName: 'Karim Ahmed',
    severity: 'low',
    reason: 'Zero hints used with 100% score on a hard problem',
    detectedAt: '2026-07-21T09:15:00Z',
  },
];

const MOCK_STUDENT_DETAILS: InstructorStudentDetail[] = MOCK_STUDENTS.map(student => ({
  ...student,
  streak: student.id === 's1' ? 14 : 7,
  hintsUsed: student.id === 's3' ? 2 : 11,
  lastActiveAt: '2026-07-23T11:30:00Z',
  topicMastery: [
    { topic: 'Arrays', percentage: student.avgScore },
    { topic: 'Recursion', percentage: Math.max(student.avgScore - 12, 30) },
    { topic: 'Graphs', percentage: Math.max(student.avgScore - 24, 25) },
  ],
  recentSubmissions: [
    {
      problemTitle: 'Two Sum',
      status: 'Accepted',
      submittedAt: '2026-07-23T10:45:00Z',
    },
    {
      problemTitle: 'Valid Parentheses',
      status: 'WrongAnswer',
      submittedAt: '2026-07-22T16:20:00Z',
    },
  ],
}));
