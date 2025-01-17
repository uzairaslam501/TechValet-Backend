using ITValet.HelpingClasses;
using ITValet.Models;
using Microsoft.EntityFrameworkCore;

namespace ITValet.Services
{
    public interface IFrequentlyAskedQuestionRepo
    {
        //Task<FrequentlyAskedQuestion?> GetFrequentlyAskedQuestionById(string id);
        //Task<IEnumerable<FrequentlyAskedQuestion>> GetFrequentlyAskedQuestionList();
        //Task<bool> AddFrequentlyAskedQuestion(AddFrequentlyAskedQuestionViewModel FrequentlyAskedQuestion);
        //Task<bool> UpdateFrequentlyAskedQuestion(UpdateFrequentlyAskedQuestionViewModel FrequentlyAskedQuestion);
        //Task<bool> DeleteFrequentlyAskedQuestion(string id);
    }

    public class FrequentlyAskedQuestionRepo : IFrequentlyAskedQuestionRepo
    {
        private readonly AppDbContext _context;
        public FrequentlyAskedQuestionRepo(AppDbContext _appDbContext)
        {
            _context = _appDbContext;
        }

        //public async Task<bool> AddFrequentlyAskedQuestion(AddFrequentlyAskedQuestionViewModel FrequentlyAskedQuestion)
        //{
        //    try
        //    {
        //        _context.FrequentlyAskedQuestions.Add(FrequentlyAskedQuestion);
        //        await _context.SaveChangesAsync();
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //public async Task<bool> DeleteFrequentlyAskedQuestion(int id)
        //{
        //    try
        //    {
        //        FrequentlyAskedQuestion? FrequentlyAskedQuestion = await GetFrequentlyAskedQuestionById(id);

        //        if (FrequentlyAskedQuestion != null)
        //        {
        //            FrequentlyAskedQuestion.IsActive = 0;
        //            FrequentlyAskedQuestion.DeletedAt = GeneralPurpose.DateTimeNow();
        //            return await UpdateFrequentlyAskedQuestion(FrequentlyAskedQuestion);
        //        }
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        //public async Task<FrequentlyAskedQuestion?> GetFrequentlyAskedQuestionById(int id)
        //{
        //    return await _context.FrequentlyAskedQuestions.FindAsync(id);
        //}

        //public async Task<IEnumerable<FrequentlyAskedQuestion>> GetFrequentlyAskedQuestionList()
        //{
        //    return await _context.FrequentlyAskedQuestions.Where(x => x.IsActive == (int)EnumActiveStatus.Active).ToListAsync();
        //}

        //public async Task<bool> UpdateFrequentlyAskedQuestion(FrequentlyAskedQuestion FrequentlyAskedQuestion)
        //{
        //    try
        //    {
        //        _context.Entry(FrequentlyAskedQuestion).State = EntityState.Modified;
        //        await _context.SaveChangesAsync();
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}
    }
}
