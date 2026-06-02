import ProtectedRoute from '@/components/ProtectedRoute';
import apiClient from '@/lib/api-client';
import { useEffect, useMemo, useState } from 'react';

interface TeachingRosterEntry {
  id: string;
  studentId: string;
  studentName: string;
  parentName?: string;
  parentEmail?: string;
  parentPhone?: string;
  courseId: string;
  courseName: string;
  courseCode?: string;
  semesterName: string;
  enrollmentType: string;
}

interface CourseRosterGroup {
  courseId: string;
  courseName: string;
  courseCode?: string;
  semesterName: string;
  entries: TeachingRosterEntry[];
}

export default function TeachingPage() {
  const [entries, setEntries] = useState<TeachingRosterEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const loadRoster = async () => {
      try {
        setLoading(true);
        setError(null);

        const response = await apiClient.get('/api/enrollments/teaching');
        if (!response.ok) {
          const payload = await response.json().catch(() => null);
          throw new Error(payload?.error || 'Failed to load your teaching rosters.');
        }

        const rosterEntries = await response.json() as TeachingRosterEntry[];
        setEntries(rosterEntries);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load your teaching rosters.');
      } finally {
        setLoading(false);
      }
    };

    void loadRoster();
  }, []);

  const courseGroups = useMemo<CourseRosterGroup[]>(() => {
    const groups = new Map<string, CourseRosterGroup>();

    for (const entry of entries) {
      const existing = groups.get(entry.courseId);
      if (existing) {
        existing.entries.push(entry);
        continue;
      }

      groups.set(entry.courseId, {
        courseId: entry.courseId,
        courseName: entry.courseName,
        courseCode: entry.courseCode,
        semesterName: entry.semesterName,
        entries: [entry]
      });
    }

    return Array.from(groups.values());
  }, [entries]);

  return (
    <ProtectedRoute roles={['Educator']}>
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900" data-testid="teaching-page-title">
            My Rosters
          </h1>
          <p className="mt-2 text-gray-600">
            View the students and parent contact details for the courses you teach.
          </p>
        </div>

        {loading && (
          <div className="rounded-lg bg-white p-6 shadow-sm" data-testid="teaching-loading-state">
            <p className="text-gray-600">Loading your rosters...</p>
          </div>
        )}

        {!loading && error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-700" data-testid="teaching-error-state">
            {error}
          </div>
        )}

        {!loading && !error && courseGroups.length === 0 && (
          <div className="rounded-lg bg-white p-6 shadow-sm" data-testid="teaching-empty-state">
            <h2 className="text-lg font-semibold text-gray-900">No assigned rosters yet</h2>
            <p className="mt-2 text-gray-600">
              You can access this page, but there are no course rosters assigned to your educator account yet.
            </p>
            <p className="mt-2 text-gray-600" data-testid="teaching-empty-reason">
              Once an administrator assigns you to a course, student and parent contact details will appear here automatically.
            </p>
            <p className="mt-2 text-sm text-gray-500" data-testid="teaching-empty-help">
              If you teach as a co-op member or invited educator and expected a roster here, contact the administrator who manages course instructor assignments for this term.
            </p>
          </div>
        )}

        {!loading && !error && courseGroups.length > 0 && (
          <div className="space-y-6" data-testid="teaching-roster-list">
            {courseGroups.map((group) => (
              <section key={group.courseId} className="rounded-lg bg-white shadow-sm" data-testid={`teaching-course-${group.courseId}`}>
                <div className="border-b border-gray-200 px-6 py-4">
                  <h2 className="text-xl font-semibold text-gray-900">{group.courseName}</h2>
                  <p className="mt-1 text-sm text-gray-600">
                    {group.courseCode ? `${group.courseCode} · ` : ''}{group.semesterName}
                  </p>
                </div>

                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wide text-gray-500">Student</th>
                        <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wide text-gray-500">Parent</th>
                        <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wide text-gray-500">Contact</th>
                        <th className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wide text-gray-500">Status</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200 bg-white">
                      {group.entries.map((entry) => (
                        <tr key={entry.id} data-testid={`teaching-roster-entry-${entry.id}`}>
                          <td className="px-6 py-4 text-sm text-gray-900">{entry.studentName}</td>
                          <td className="px-6 py-4 text-sm text-gray-700">{entry.parentName || 'Unavailable'}</td>
                          <td className="px-6 py-4 text-sm text-gray-700">
                            <div>{entry.parentEmail || 'No email'}</div>
                            <div>{entry.parentPhone || 'No phone'}</div>
                          </td>
                          <td className="px-6 py-4 text-sm text-gray-700">{entry.enrollmentType}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            ))}
          </div>
        )}
      </main>
    </ProtectedRoute>
  );
}