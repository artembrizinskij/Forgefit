import { InMemoryDatabase } from './inMemory.js'
import type { IDatabase } from './interface.js'

/**
 * Database singleton.
 *
 * To switch to a real database:
 * 1. Create a new class that implements IDatabase (e.g. PostgresDatabase)
 * 2. Replace `new InMemoryDatabase()` below with your implementation
 * 3. Add any connection/init logic (connect(), migrate(), etc.)
 *
 * Example connectors to add later:
 *   - PostgreSQL:  import { PostgresDatabase } from './postgres.js'
 *   - MongoDB:     import { MongoDatabase }    from './mongo.js'
 *   - SQLite:      import { SqliteDatabase }   from './sqlite.js'
 */
export const db: IDatabase = new InMemoryDatabase()
