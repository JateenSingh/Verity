using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Verity.Application.Abstractions;
using Verity.Domain.Entities;
using Verity.Domain.Enums;

namespace Verity.Infrastructure.Seed;

/// <summary>
/// Idempotent, deterministic seed data: skips entirely if any user already
/// exists. Content is generated from a fixed Random seed so re-running
/// against a fresh database always produces the same forum.
/// </summary>
public sealed class DbSeeder(VerityDbContext db, IPasswordHasher hasher, IClock clock, IConfiguration configuration)
{
    private const int RandomSeed = 20260916;

    public async Task SeedAsync(CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var sharedPassword = configuration["Seed:Password"]
            ?? throw new InvalidOperationException(
                "Seed:Password is not configured. Set the SEED__PASSWORD environment variable (shared separately) before seeding.");

        var rng = new Random(RandomSeed);
        var now = clock.UtcNow;
        var passwordHash = hasher.Hash(sharedPassword);

        var users = CreateUsers(passwordHash, now);
        db.Users.AddRange(users.Values);

        // naledi is deliberately excluded from post authorship - she exists
        // to exercise the "author filter with zero posts" case.
        var postAuthors = users.Values.Where(u => u.Username != "naledi").ToList();
        var allActiveUsers = users.Values.ToList();
        var moderators = new[] { users["mod_alice"], users["mod_bob"] };

        var posts = CreatePosts(postAuthors, rng, now);
        var likeCounts = AssignLikeCounts(posts.Count, rng);

        for (var i = 0; i < posts.Count; i++)
        {
            db.Entry(posts[i]).Property(p => p.LikeCount).CurrentValue = likeCounts[i];
        }

        db.Posts.AddRange(posts);

        var comments = CreateComments(posts, allActiveUsers, rng);
        db.Comments.AddRange(comments);

        var likes = CreateLikes(posts, allActiveUsers, likeCounts, rng);
        db.Likes.AddRange(likes);

        var tags = CreateTags(posts, moderators, now);
        db.PostTags.AddRange(tags);

        await db.SaveChangesAsync(ct);
    }

    private static Dictionary<string, User> CreateUsers(string passwordHash, DateTimeOffset now)
    {
        (string Username, string Email, UserRole Role, string IdSuffix)[] specs =
        [
            ("mod_alice", "alice@iidentifii.test", UserRole.Moderator, "000000000001"),
            ("mod_bob", "bob@iidentifii.test", UserRole.Moderator, "000000000002"),
            ("jateen", "jateen@iidentifii.test", UserRole.User, "000000000003"),
            ("partner_acme", "dev@acme.test", UserRole.User, "000000000004"),
            ("partner_globex", "dev@globex.test", UserRole.User, "000000000005"),
            ("sipho", "sipho@iidentifii.test", UserRole.User, "000000000006"),
            ("naledi", "naledi@iidentifii.test", UserRole.User, "000000000007"),
        ];

        var users = new Dictionary<string, User>();
        foreach (var (username, email, role, idSuffix) in specs)
        {
            var id = Guid.Parse($"00000000-0000-0000-0000-{idSuffix}");
            users[username] = User.Create(username, email, passwordHash, role, id, now.AddDays(-120));
        }

        return users;
    }

    private static readonly (string Title, string Body)[] PostTemplates =
    [
        ("Getting a 401 from the auth endpoint after token refresh", "Our mobile client refreshes the access token every 45 minutes, but every third or fourth refresh the next API call comes back 401 even though the new token looks valid when I decode it. Anyone seen clock skew cause this?"),
        ("Liveness check keeps timing out on older Android devices", "We're seeing liveness sessions time out specifically on devices running Android 9 and below. Camera permissions are granted and the preview renders fine, but the challenge never completes. Is there a minimum camera API level we should be gating on?"),
        ("Webhook retries firing 6+ times for the same verification event", "Our endpoint returns 200 within 300ms, but we're still getting the same webhook.verification.completed event delivered 6 times over about 10 minutes. Is there a dedup key we should be checking, or is this expected retry behaviour?"),
        ("OCR misreading date of birth on UK driving licences", "Date of birth is coming back with the day and month swapped on maybe 1 in 20 UK driving licence scans. Passport OCR for the same users is fine. Anyone else seeing this on the current model version?"),
        ("SDK throws SDK_INIT_TIMEOUT intermittently on first launch", "About 5% of cold starts throw SDK_INIT_TIMEOUT before any liveness UI is shown. Warm starts never show it. Network looks fine in device logs. Any known interaction with app-startup prefetching?"),
        ("Best way to pre-warm the liveness camera session?", "We want to shave a second or two off the perceived start time of the liveness flow. Is there a supported way to initialize the camera session before the user taps 'Start verification', or does that risk permission timing issues?"),
        ("Document capture rejects valid passports with glare warning", "Several users with laminated passport photo pages are getting a persistent glare warning even under diffuse lighting. Is there a way to tune the glare threshold, or is manual capture override the only option?"),
        ("Rate limit on the verification-status polling endpoint?", "We poll GET /verifications/{id} every 2 seconds while waiting for a result. Is there an official rate limit we should be respecting, or should we move to the webhook instead of polling entirely?"),
        ("Sandbox environment returns different confidence scores than prod", "The same test images give noticeably different liveness confidence scores in sandbox vs production. Is the sandbox model intentionally a different version, and if so is that documented anywhere?"),
        ("Face match fails consistently for users wearing glasses", "We're seeing a higher false-rejection rate for users wearing prescription glasses during the face-match step, even with anti-glare coating. Is there a recommended UX nudge to ask users to remove glasses first?"),
        ("Integration question: can we brand the liveness capture screen?", "Looking to match the liveness capture UI to our app's colour scheme and add our logo to the header. Is there a theming API for the native SDKs, or is this only configurable via the web SDK?"),
        ("Webhook signature verification failing after key rotation", "We rotated our webhook signing secret through the dashboard and now every incoming webhook fails HMAC verification, even ones that should be using the new secret. Is there a propagation delay after rotation?"),
        ("Best practice for handling verification abandonment mid-flow?", "A meaningful chunk of users close the app partway through document capture. Is there a webhook or status we can poll for 'abandoned' so we can prompt them to resume later?"),
        ("SDK bundle size increased significantly after last update", "After updating to the latest SDK version, our Android app's APK grew by about 8MB. Is that expected from the new model files, and is there a way to lazy-load them instead of bundling at build time?"),
        ("Document OCR returns null for MRZ on damaged passports", "For passports with a partially torn or worn machine-readable zone, OCR returns null for the whole MRZ block instead of the fields it could still read. Is partial MRZ extraction supported at all?"),
        ("Clarifying the difference between verification statuses", "The docs list pending, processing, approved, rejected and needs_review, but it's not clear which of those are terminal vs which can still transition. Is there a state diagram somewhere?"),
        ("Liveness challenge instructions not localizing correctly", "We've set the locale to pt-BR in the SDK config, but the on-screen liveness instructions are still showing in English while the rest of our app is correctly localized. Anyone hit this?"),
        ("Handling users who fail liveness three times in a row", "After three failed liveness attempts we currently just show a generic error. Is there a recommended fallback flow (e.g. manual review, different capture method) that other integrators use here?"),
        ("Webhook payload missing the applicant reference we set at creation", "We pass a custom applicantReference when creating a verification, but it's absent from the completed webhook payload even though it shows up fine in the dashboard. Known issue?"),
        ("Does the SDK cache captured images locally between sessions?", "For a compliance review we need to confirm whether captured document images are ever persisted to local device storage outside of the active session, even temporarily.")
    ];

    private static List<Post> CreatePosts(IReadOnlyList<User> authors, Random rng, DateTimeOffset now)
    {
        const int weeks = 13;
        var posts = new List<Post>();

        // ISO week (Monday-Sunday) containing "now", as a UTC midnight anchor.
        var daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
        var thisWeekMonday = new DateTimeOffset(now.Date, TimeSpan.Zero).AddDays(-daysSinceMonday);

        // Baseline: 3 posts placed inside each of the last 13 real calendar
        // weeks (Mon 00:00 - Sun 23:59), not a naive rolling 7-day window,
        // so date_trunc('week', created_at) actually buckets >=3 per week.
        for (var week = 0; week < weeks; week++)
        {
            var weekMonday = thisWeekMonday.AddDays(-7 * week);

            foreach (var dayWithinWeek in (int[]) [1, 3, 5])
            {
                var author = authors[rng.Next(authors.Count)];
                var template = PostTemplates[rng.Next(PostTemplates.Length)];
                var createdAt = weekMonday.AddDays(dayWithinWeek).AddHours(rng.Next(6, 20));

                posts.Add(Post.Create(author.Id, template.Title, template.Body, createdAt: createdAt));
            }
        }

        // Five posts sharing the exact same instant (inside week 6, so it
        // doesn't disturb any week's >=3 minimum), to exercise the sort
        // tie-break (createdAt desc, id desc).
        var tieInstant = thisWeekMonday.AddDays(-7 * 6).AddDays(2).AddHours(11).AddMinutes(30);
        for (var i = 0; i < 5; i++)
        {
            var author = authors[rng.Next(authors.Count)];
            var template = PostTemplates[rng.Next(PostTemplates.Length)];
            posts.Add(Post.Create(author.Id, template.Title, template.Body, createdAt: tieInstant));
        }

        return posts;
    }

    private static int[] AssignLikeCounts(int postCount, Random rng)
    {
        var counts = new int[postCount];
        counts[0] = 6;
        counts[1] = 5;
        counts[2] = 4;

        var remaining = 150 - (6 + 5 + 4);
        for (var i = 3; i < postCount && remaining > 0; i++)
        {
            var c = Math.Min(remaining, rng.Next(2, 6));
            counts[i] = c;
            remaining -= c;
        }

        if (remaining > 0)
        {
            counts[postCount - 1] = Math.Min(6, counts[postCount - 1] + remaining);
        }

        return counts;
    }

    private static readonly string[] CommentTemplates =
    [
        "We hit the same thing last month - turned out to be a proxy stripping a header. Worth checking your egress config.",
        "Can confirm this on our side too. Opened a support ticket, will report back if I hear anything.",
        "Have you tried the latest SDK patch release? There was a fix for something that sounds similar.",
        "This happened to us only in sandbox, never in production - might be environment specific.",
        "+1, seeing this as well. Following for updates.",
        "We worked around it by adding a short retry with backoff on our side, not ideal but it's stable now.",
        "Is this still happening on the newest release? We haven't seen it since upgrading.",
        "Thanks for posting this - saved me a couple of hours of debugging on our integration.",
        "We reached out to support directly and they confirmed it's a known issue being worked on.",
        "Same symptoms here but only on iOS, Android has been fine for us.",
    ];

    private static List<Comment> CreateComments(IReadOnlyList<Post> posts, IReadOnlyList<User> commenters, Random rng)
    {
        var comments = new List<Comment>();
        const int totalBudget = 120;
        var remaining = totalBudget;

        // Two posts get a heavy comment thread (12+) to exercise paging.
        var hotIndices = new[] { 0, 1 };
        foreach (var hotIndex in hotIndices)
        {
            var count = 13;
            remaining -= count;
            AddComments(posts[hotIndex], count);
        }

        for (var i = 2; i < posts.Count && remaining > 0; i++)
        {
            var count = Math.Min(remaining, rng.Next(0, 6));
            remaining -= count;
            AddComments(posts[i], count);
        }

        return comments;

        void AddComments(Post post, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var author = commenters[rng.Next(commenters.Count)];
                var body = CommentTemplates[rng.Next(CommentTemplates.Length)];
                var createdAt = post.CreatedAt.AddHours(i + 1).AddMinutes(rng.Next(0, 59));
                comments.Add(Comment.Create(post.Id, author.Id, body, createdAt: createdAt));
            }
        }
    }

    private static List<Like> CreateLikes(IReadOnlyList<Post> posts, IReadOnlyList<User> users, int[] likeCounts, Random rng)
    {
        var likes = new List<Like>();

        for (var i = 0; i < posts.Count; i++)
        {
            var post = posts[i];
            var target = likeCounts[i];
            var eligible = users.Where(u => u.Id != post.AuthorId).OrderBy(_ => rng.Next()).Take(target).ToList();

            foreach (var liker in eligible)
            {
                var createdAt = post.CreatedAt.AddMinutes(rng.Next(10, 600));
                likes.Add(Like.Create(post.Id, liker.Id, createdAt));
            }
        }

        return likes;
    }

    private static List<PostTagAssignment> CreateTags(IReadOnlyList<Post> posts, IReadOnlyList<User> moderators, DateTimeOffset now)
    {
        string[] reasons =
        [
            "Contradicts the published SDK behaviour for this endpoint.",
            "Claims a rate limit that does not match our documented limits.",
            "Describes a workaround that bypasses required compliance checks.",
            "Misattributes a client-side bug to the verification model.",
            "References a deprecated API that no longer behaves this way.",
            "Overstates data retention behaviour beyond what is documented.",
        ];

        var taggers = new[] { moderators[0], moderators[0], moderators[0], moderators[0], moderators[1], moderators[1] };
        var tags = new List<PostTagAssignment>();

        for (var i = 0; i < 6; i++)
        {
            var post = posts[posts.Count - 1 - i];
            tags.Add(PostTagAssignment.Create(post.Id, PostTag.MisleadingOrFalse, taggers[i].Id, reasons[i], now.AddDays(-i)));
        }

        return tags;
    }
}
